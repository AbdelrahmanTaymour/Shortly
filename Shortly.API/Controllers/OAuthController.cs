using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.ServiceContracts.Authentication;

namespace Shortly.API.Controllers;

/// <summary>
///     Controller for handling OAuth authentication with external providers
/// </summary>
[ApiController]
[Route("api/oauth")]
[Produces("application/json")]
public class OAuthController(
    IOAuthService oAuthService,
    ILogger<OAuthController> logger,
    IConfiguration configuration
) : ControllerApiBase
{
    /// <summary>
    ///     Initiates Google OAuth login flow
    /// </summary>
    /// <param name="returnUrl">Optional return URL after successful authentication</param>
    /// <returns>Redirects to Google OAuth consent screen</returns>
    /// <remarks>
    ///     This endpoint initiates the OAuth 2.0 authorization code flow with Google.
    ///     The user will be redirected to Google's login page, and upon successful authentication,
    ///     will be redirected back to the callback endpoint.
    ///     Example: GET /api/oauth/google/login?returnUrl=/dashboard
    /// </remarks>
    /// <response code="302">Redirects to Google OAuth consent screen</response>
    [HttpGet("google/login", Name = "GoogleLogin")]
    [AllowAnonymous]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "OAuth", new { returnUrl });
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            Items = { { "scheme", GoogleDefaults.AuthenticationScheme } }
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    ///     Handles Google OAuth callback and completes authentication
    /// </summary>
    /// <param name="returnUrl">Optional return URL to redirect after authentication</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Redirects to frontend with authentication tokens</returns>
    /// <remarks>
    ///     Google calls this endpoint after the user grants permission.
    ///     It exchanges the authorization code for user information and creates/authenticates the user.
    ///     The user will be redirected to the frontend with tokens in the URL hash or query parameters.
    /// </remarks>
    /// <response code="302">Redirects to frontend application with authentication tokens</response>
    /// <response code="400">OAuth authentication failed</response>
    [HttpGet("google/callback", Name = "GoogleCallback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Authenticate with Google
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                logger.LogWarning("Google authentication failed");
                return RedirectToFrontendWithError("Authentication failed");
            }

            var claims = authenticateResult.Principal.Claims.ToList();

            // Extract user information from claims
            var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var picture = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                logger.LogWarning("Missing required claims from Google OAuth");
                return RedirectToFrontendWithError("Missing required information from Google");
            }

            // Authenticate or register a user
            var authResponse = await oAuthService.AuthenticateWithGoogleAsync(
                email,
                googleId,
                name,
                picture,
                cancellationToken);

            if (authResponse == null || !authResponse.Success)
            {
                logger.LogWarning("OAuth service authentication failed for {Email}", email);
                return RedirectToFrontendWithError("Authentication failed");
            }

            // Redirect to the frontend with tokens
            return RedirectToFrontendWithTokens(authResponse, returnUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Google OAuth callback");
            return RedirectToFrontendWithError("An unexpected error occurred");
        }
    }

    /// <summary>
    ///     Redirects to frontend with authentication tokens
    /// </summary>
    private IActionResult RedirectToFrontendWithTokens(AuthenticationResponse authResponse, string? returnUrl)
    {
        var frontendUrl = configuration["AppSettings:BaseUIUrl"] ?? "http://localhost:3000";

        // Using URL hash to avoid tokens appearing in server logs
        var callbackUrl = $"{frontendUrl}/callback.html#{BuildTokenQueryString(authResponse)}";

        return Redirect(callbackUrl);
    }

    /// <summary>
    ///     Redirects to frontend with error message
    /// </summary>
    private IActionResult RedirectToFrontendWithError(string errorMessage)
    {
        var frontendUrl = configuration["AppSettings:BaseUIUrl"] ?? "http://localhost:3000";
        var callbackUrl = $"{frontendUrl}/auth/callback?error={Uri.EscapeDataString(errorMessage)}";

        return Redirect(callbackUrl);
    }

    /// <summary>
    ///     Builds query string from authentication response
    /// </summary>
    private string BuildTokenQueryString(AuthenticationResponse authResponse)
    {
        var parameters = new Dictionary<string, string>
        {
            ["access_token"] = authResponse.Tokens?.AccessToken ?? "",
            ["refresh_token"] = authResponse.Tokens?.RefreshToken ?? "",
            ["token_type"] = "Bearer",
            ["expires_at"] = authResponse.Tokens?.AccessTokenExpiry.ToString("o") ?? ""
        };

        return string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
    }

    /// <summary>
    ///     Gets the current authenticated user's OAuth connection status
    /// </summary>
    /// <returns>Information about connected OAuth providers</returns>
    /// <response code="200">Returns OAuth connection status</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("status", Name = "OAuthStatus")]
    [Authorize]
    public IActionResult GetOAuthStatus()
    {
        throw new NotImplementedException();
    }
}
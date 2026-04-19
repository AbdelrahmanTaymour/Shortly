using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Controllers.Base;
using Shortly.Core.Auth.Contracts;

namespace Shortly.API.Controllers;

/// <summary>
///     Handles OAuth authentication with external providers.
///     SECURITY MODEL (BFF — Backend For Frontend):
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
    ///     Initiates the Google OAuth 2.0 authorization code flow.
    ///     Redirects the browser to Google's consent screen.
    /// </summary>
    /// <param name="returnUrl">
    ///     Optional path to redirect to after successful authentication
    ///     (e.g. "/dashboard", "/settings"). Must be a local path.
    /// </param>
    /// <response code="302">Redirects to Google's OAuth consent screen.</response>
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
    ///     Google calls this after the user grants permission.
    ///     Authenticates/creates the user, sets the HttpOnly refresh-token
    ///     cookie, then performs a clean redirect to the frontend — no tokens
    ///     in the URL.
    /// </summary>
    /// <param name="returnUrl">Forwarded from GoogleLogin.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="302">
    ///     Success → redirects to the frontend (returnUrl or "/").
    ///     Failure → redirects to /login?error=…
    /// </response>
    [HttpGet("google/callback", Name = "GoogleCallback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Exchange the authorization code for user claims
            var authResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authResult.Succeeded)
            {
                logger.LogWarning("Google OAuth failed: {Reason}", authResult.Failure?.Message);
                return RedirectToFrontendWithError("Google authentication failed. Please try again.");
            }

            // Extract required claims
            var claims = authResult.Principal!.Claims.ToList();
            var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var picture = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                logger.LogWarning("Google OAuth: required claims (sub / email) are missing");
                return RedirectToFrontendWithError("Required account information was not returned by Google.");
            }

            // Authenticate / create / link the user in our system
            var authResponse = await oAuthService.AuthenticateWithGoogleAsync(
                email, googleId, name, picture, cancellationToken);

            if (authResponse is null || !authResponse.Success)
            {
                logger.LogWarning("OAuthService could not authenticate Google user {Email}", email);
                return RedirectToFrontendWithError("Authentication failed. Please try again.");
            }

            if (authResponse.Tokens?.RefreshToken is null)
            {
                logger.LogError("TokenService returned no refresh token for OAuth user {Email}", email);
                return RedirectToFrontendWithError("Token generation failed. Please try again.");
            }

            // Persist the refresh token as an HttpOnly cookie
            //       The access token is NOT sent to the browser here.
            //       The frontend will request one via POST /api/auth/refresh-token.
            SetRefreshTokenCookie(authResponse.Tokens.RefreshToken);

            // Clean redirect
            logger.LogInformation("Google OAuth successful for user {Email}", email);
            return RedirectToFrontend(returnUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error during Google OAuth callback");
            return RedirectToFrontendWithError("An unexpected error occurred. Please try again.");
        }
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    /// <summary>
    ///     Redirects the browser to the frontend after a successful OAuth flow.
    ///     Accepts an optional <paramref name="returnUrl" /> so users land on the
    ///     page they were trying to reach before being sent to Google.
    ///     Security: only local paths are allowed — a full URL (e.g. from a
    ///     malicious returnUrl parameter) is silently replaced with "/".
    /// </summary>
    private IActionResult RedirectToFrontend(string? returnUrl)
    {
        var frontendUrl = GetFrontendUrl();
        var destination = IsLocalPath(returnUrl) ? returnUrl! : "/";
        return Redirect($"{frontendUrl}{destination}");
    }

    /// <summary>
    ///     Redirects the browser to /login with a human-readable error message
    ///     in the query string so the login page can display it.
    /// </summary>
    private IActionResult RedirectToFrontendWithError(string errorMessage)
    {
        var frontendUrl = GetFrontendUrl();
        return Redirect($"{frontendUrl}/login?error={Uri.EscapeDataString(errorMessage)}");
    }

    /// <summary>
    ///     Sets the refresh token as an HttpOnly cookie with settings that
    ///     match those in <see cref="AuthController" /> exactly, so the
    ///     /api/auth/refresh-token and /api/auth/logout endpoints can read
    ///     and delete the cookie regardless of which flow created it.
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        HttpContext.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true, // JS cannot read this — XSS protection
            Secure = true, // HTTPS only
            SameSite = SameSiteMode.None, // Required: frontend & API are on different origins
            Path = "/", // Must match the Path used by AuthController
            Expires = DateTime.UtcNow.AddDays(7),
            IsEssential = true // GDPR: session management is essential
        });
    }

    private string GetFrontendUrl()
    {
        return configuration["AppSettings:BaseUIUrl"] ?? "http://localhost:3000";
    }

    /// <summary>
    ///     Returns true only for local paths (starts with "/" but not "//").
    ///     Prevents open-redirect attacks via a crafted returnUrl.
    /// </summary>
    private static bool IsLocalPath(string? url)
    {
        return !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//");
    }
}
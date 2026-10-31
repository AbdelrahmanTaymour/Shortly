using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Core.Account.GoogleLogin;

namespace Shortly.API.Controllers.OAuth;

[ApiController]
[Route("api/oauth")]
[Produces("application/json")]
public class OAuthController(
    ISender sender,
    ILogger<OAuthController> logger,
    IConfiguration configuration) : ControllerBase
{
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

    [HttpGet("google/callback", Name = "GoogleCallback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var authResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authResult.Succeeded)
        {
            logger.LogWarning("Google OAuth failed: {Reason}", authResult.Failure?.Message);
            return RedirectToFrontendWithError("Google authentication failed. Please try again.");
        }

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

        var command = new GoogleLoginCommand(googleId, email, name, picture);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Google login failed for {Email}: {Error}", email, result.Error.Message);
            return RedirectToFrontendWithError("Authentication failed. Please try again.");
        }

        logger.LogInformation("Google OAuth successful for user {Email}", email);
        return RedirectToFrontend(returnUrl);
    }

    private IActionResult RedirectToFrontend(string? returnUrl)
    {
        var frontendUrl = GetFrontendUrl();
        var destination = IsLocalPath(returnUrl) ? returnUrl! : "/";
        return Redirect($"{frontendUrl}{destination}");
    }

    private IActionResult RedirectToFrontendWithError(string errorMessage)
    {
        var frontendUrl = GetFrontendUrl();
        return Redirect($"{frontendUrl}/login?error={Uri.EscapeDataString(errorMessage)}");
    }

    private string GetFrontendUrl() =>
        configuration["AppSettings:BaseUIUrl"] ?? "http://localhost:3000";

    private static bool IsLocalPath(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//");
}
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shortly.Api.Extensions;
using Shortly.Api.HTMLTemplates;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.RedirectToShortUrl;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;
using Shortly.Infrastructure.EdgeProtection.Models;
using Shortly.Infrastructure.EdgeProtection.Services;

namespace Shortly.Api.Controllers.ShortUrlRedirect;

[ApiController]
[Route("")]
[Produces("application/json")]
public class ShortUrlRedirectController(
    ISender sender, 
    IJwtService jwtService, 
    LinkPasswordAttemptLimiter linkPasswordAttempts) : ControllerBase
{
    /// <summary>Handles redirection for short URLs, prompting for a password if the link is protected.</summary>
    [HttpGet("{shortCode}", Name = "RedirectToUrl")]
    [AllowAnonymous]
    public async Task<IActionResult> RedirectToUrl(string shortCode, CancellationToken cancellationToken = default)
    {
        if (shortCode.Length > MaxShortCodeLength) 
            return Result.Failure(ShortUrlErrors.NotFound).ToProblemDetails();

        var result = await sender.Send(BuildRedirectCommand(shortCode, password: null), cancellationToken);

        if (result.IsSuccess) 
            return Redirect(result.Value);

        // If a password is required, generate a temporary signed token and redirect to the password page
        if (result.Error == ShortUrlErrors.PasswordRequired)
        {
            var token = jwtService.GenerateRedirectToken(shortCode, TimeSpan.FromMinutes(5));
            return RedirectToAction(nameof(ShowPasswordPage), new { token });
        }

        return result.ToProblemDetails();
    }

    /// <summary>Renders the password entry HTML page for protected short links.</summary>
    [HttpGet("api/url-redirect/password-page", Name = "ShowPasswordPage")]
    [AllowAnonymous]
    public IActionResult ShowPasswordPage([FromQuery] string token, string? error = null)
    {
        var shortCode = jwtService.ValidateRedirectToken(token);
        if (shortCode is null) return Forbid();

        return Content(UrlPasswordFormTemplate.Generate(error, token), "text/html");
    }

    /// <summary>Verifies the submitted password and redirects if correct, or rate-limits if abused.</summary>
    [HttpPost("api/url-redirect/verify", Name = "VerifyUrlPassword")]
    [AllowAnonymous]
    [EnableRateLimiting(PasswordVerificationRateLimitOptions.PolicyName)]
    public async Task<IActionResult> VerifyPassword(
        [FromForm] string token, 
        [FromForm] string password, 
        CancellationToken cancellationToken = default)
    {
        var shortCode = jwtService.ValidateRedirectToken(token);
        if (shortCode is null) return Forbid();

        // Check if this specific link has been locked out due to too many failed password attempts
        if (linkPasswordAttempts.IsLocked(shortCode, out var retryAfter))
        {
            if (retryAfter is { } wait) 
                Response.Headers.RetryAfter = ((int)Math.Ceiling(wait.TotalSeconds)).ToString();
            
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return Content(
                UrlPasswordFormTemplate.Generate("Too many failed password attempts for this link. Try again later.", token),
                "text/html");
        }

        var result = await sender.Send(BuildRedirectCommand(shortCode, password), cancellationToken);

        // Only record failures against the link budget for incorrect passwords
        if (result.IsFailure && result.Error == ShortUrlErrors.InvalidPassword) 
            linkPasswordAttempts.RecordFailure(shortCode);

        return result.IsFailure
            ? ShowPasswordPage(token, "Invalid password. Please try again.")
            : Redirect(result.Value);
    }

    private RedirectToShortUrlCommand BuildRedirectCommand(string shortCode, string? password)
    {
        var query = Request.Query;

        return new RedirectToShortUrlCommand(
            shortCode,
            password,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            SessionId: ResolveSessionId(),
            // Truncate tracking headers to match database column limits safely
            UserAgent: Truncate(Request.Headers.UserAgent.ToString(), MaxUserAgentLength) ?? string.Empty,
            Referrer: Request.Headers.Referer is { Count: > 0 } referer ? Truncate(referer.ToString(), MaxReferrerLength) : null,
            UtmSource: Truncate(query["utm_source"].FirstOrDefault(), MaxUtmLength),
            UtmMedium: Truncate(query["utm_medium"].FirstOrDefault(), MaxUtmLength),
            UtmCampaign: Truncate(query["utm_campaign"].FirstOrDefault(), MaxUtmLength),
            UtmTerm: Truncate(query["utm_term"].FirstOrDefault(), MaxUtmLength),
            UtmContent: Truncate(query["utm_content"].FirstOrDefault(), MaxUtmLength));
    }

    private const int MaxShortCodeLength = 50;
    private const int MaxUserAgentLength = 500;
    private const int MaxReferrerLength = 500;
    private const int MaxUtmLength = 100;
    private const int MaxSessionIdLength = 128;

    /// <summary>Safely truncates a string to a maximum length without splitting a surrogate pair.</summary>
    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null || value.Length <= maxLength) return value;

        var length = char.IsHighSurrogate(value[maxLength - 1]) ? maxLength - 1 : maxLength;
        return value[..length];
    }

    private string ResolveSessionId()
    {
        const string cookieName = "shortly_sid";

        // Reuse existing valid session cookie if present
        if (Request.Cookies.TryGetValue(cookieName, out var existing) && 
            !string.IsNullOrWhiteSpace(existing) && 
            existing.Length <= MaxSessionIdLength)
            return existing;

        // Otherwise, issue a new long-lived session cookie for analytics tracking
        var sessionId = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(cookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });

        return sessionId;
    }
}
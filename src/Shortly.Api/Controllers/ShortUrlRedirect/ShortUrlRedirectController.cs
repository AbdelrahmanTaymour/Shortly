using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.API.HTMLTemplates;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.RedirectToShortUrl;
using Shortly.Domain.ShortUrls;

namespace Shortly.API.Controllers.ShortUrlRedirect;

[ApiController]
[Route("")]
[Produces("application/json")]
public class ShortUrlRedirectController(ISender sender, IJwtService jwtService) : ControllerBase
{
    [HttpGet("{shortCode}", Name = "RedirectToUrl")]
    [AllowAnonymous]
    public async Task<IActionResult> RedirectToUrl(string shortCode, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(BuildRedirectCommand(shortCode, password: null), cancellationToken);

        if (result.IsSuccess) return Redirect(result.Value);

        if (result.Error == ShortUrlErrors.PasswordRequired)
        {
            var token = jwtService.GenerateRedirectToken(shortCode, TimeSpan.FromMinutes(5));
            return RedirectToAction(nameof(ShowPasswordPage), new { token });
        }

        return result.ToProblemDetails();
    }

    [HttpGet("api/url-redirect/password-page", Name = "ShowPasswordPage")]
    [AllowAnonymous]
    public IActionResult ShowPasswordPage([FromQuery] string token, string? error = null)
    {
        var shortCode = jwtService.ValidateRedirectToken(token);
        if (shortCode is null) return Forbid();

        return Content(UrlPasswordFormTemplate.Generate(error, token), "text/html");
    }

    [HttpPost("api/url-redirect/verify", Name = "VerifyUrlPassword")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPassword([FromForm] string token, [FromForm] string password, CancellationToken cancellationToken = default)
    {
        var shortCode = jwtService.ValidateRedirectToken(token);
        if (shortCode is null) return Forbid();

        var result = await sender.Send(BuildRedirectCommand(shortCode, password), cancellationToken);

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
            UserAgent: Request.Headers.UserAgent.ToString(),
            Referrer: Request.Headers.Referer is { Count: > 0 } referer ? referer.ToString() : null,
            UtmSource: query["utm_source"].FirstOrDefault(),
            UtmMedium: query["utm_medium"].FirstOrDefault(),
            UtmCampaign: query["utm_campaign"].FirstOrDefault(),
            UtmTerm: query["utm_term"].FirstOrDefault(),
            UtmContent: query["utm_content"].FirstOrDefault());
    }

    private string ResolveSessionId()
    {
        // A lightweight, non-auth session id — purely to group clicks from the
        // same browser for bounce-rate/returning-visitor analytics.
        const string cookieName = "shortly_sid";

        if (Request.Cookies.TryGetValue(cookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
            return existing;

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
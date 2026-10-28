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
        var result = await sender.Send(new RedirectToShortUrlCommand(shortCode, null), cancellationToken);

        if (result.IsSuccess)
            return Redirect(result.Value);

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

        var result = await sender.Send(new RedirectToShortUrlCommand(shortCode, password), cancellationToken);

        return result.IsFailure
            ? ShowPasswordPage(token, "Invalid password. Please try again.")
            : Redirect(result.Value);
    }
}
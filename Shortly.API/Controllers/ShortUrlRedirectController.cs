using Microsoft.AspNetCore.Mvc;
using Shortly.API.Controllers.Base;
using Shortly.API.HTMLTemplates;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Core.ServiceContracts.UrlManagement;

namespace Shortly.API.Controllers;

/// <summary>
/// Handles the redirection logic for short URLs, including support for
/// password-protected short links using temporary redirect tokens.
/// </summary>
[ApiController]
[Route("")]
public class ShortUrlRedirectController(IShortUrlRedirectService redirectService, ITokenService tokenService) : ControllerApiBase
{
    
    /// <summary>
    /// Resolves a short code to its original URL or initiates the password-protected redirect flow.
    /// </summary>
    /// <param name="shortCode">The unique code associated with the shortened URL.</param>
    /// <param name="cancellationToken">A token for request cancellation.</param>
    /// <returns>
    /// - 301 redirect to the original URL for public links.<br/>
    /// - 302 redirect to the password entry page for protected links.
    /// </returns>
    /// <response code="301">Permanent redirect to the original URL.</response>
    /// <response code="302">Redirect to password entry page for protected URLs.</response>
    /// <response code="404">No URL found for the specified short code.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet("{shortCode}")]
    [ProducesResponseType(StatusCodes.Status301MovedPermanently)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RedirectToUrl(string shortCode, CancellationToken cancellationToken)
    {
        var result = await redirectService.GetRedirectInfoByShortCodeAsync(shortCode, cancellationToken);

        if (result.RequiresPassword)
        {
            var token = tokenService.GenerateRedirectToken(shortCode, TimeSpan.FromMinutes(5));
            return RedirectToAction(nameof(ShowPasswordPage), new { token });
        }

        return RedirectPermanent(result.OriginalUrl);
    }
    
    
    /// <summary>
    /// Renders the password entry page for a protected short URL.
    /// </summary>
    /// <param name="token">
    /// A short-lived redirect token (JWT) issued by <see cref="RedirectToUrl"/>.<br/>
    /// This token grants temporary access to the password entry form.
    /// </param>
    /// <param name="error">Optional error message displayed to the user.</param>
    /// <returns>HTML content containing the password input form.</returns>
    /// <response code="200">Password form displayed successfully.</response>
    /// <response code="403">Token is invalid, expired, or missing.</response>
    [HttpGet("api/url-redirect/password-page")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult ShowPasswordPage([FromQuery] string token, string? error = null)
    {
        var shortCode = tokenService.ValidateRedirectToken(token);
        if (shortCode == null)
            return Forbid();
        
        return Content(PasswordFormTemplate.Generate(error, token), "text/html");
    }
    
    
    /// <summary>
    /// Verifies the submitted password for a protected short URL and redirects on success.
    /// </summary>
    /// <param name="token">
    /// The short-lived redirect token identifying the short code in question.
    /// </param>
    /// <param name="password">The user-entered password.</param>
    /// <param name="cancellationToken">A token for request cancellation.</param>
    /// <returns>
    /// - 301 redirect to the original URL if the password is correct.<br/>
    /// - Re-renders the password form with an error if incorrect.
    /// </returns>
    /// <response code="301">Password correct — redirected to the original URL.</response>
    /// <response code="200">Password incorrect — form re-rendered with error.</response>
    /// <response code="403">Invalid or expired redirect token.</response>
    [HttpPost("api/url-redirect/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status301MovedPermanently)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ValidatePasswordAndRedirect(
        [FromForm] string token,
        [FromForm] string password,
        CancellationToken cancellationToken = default)
    {
        var shortCode = tokenService.ValidateRedirectToken(token);
        if (shortCode == null)
            return Forbid();
        
        if (string.IsNullOrWhiteSpace(password))
            return ShowPasswordPage(token, "Password is required");

        var originalUrl = await redirectService.GetUrlIfPasswordCorrectAsync(shortCode, password, cancellationToken);
        if (originalUrl is null)
        {
            return ShowPasswordPage(token, "Invalid password. Please try again.");
        }

        return RedirectPermanent(originalUrl);
    }
}
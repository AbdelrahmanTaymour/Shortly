using Microsoft.AspNetCore.Http;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Models;

namespace Shortly.Core.ServiceContracts.UrlManagement;

/// <summary>
/// Define operations for retrieving and managing redirect-related information
/// for shortened URLs, including password verification, click tracking,
/// and validity checks.
/// </summary>
public interface IShortUrlRedirectService
{
    /// <summary>
    /// Retrieves redirect information for a short URL by its short code.
    /// </summary>
    /// <param name="shortCode">The unique short code associated with the shortened URL.</param>
    /// <param name="context"></param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// An <see cref="UrlRedirectResult"/> containing the original URL and whether it requires a password.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when no short URL is found for the specified short code.</exception>
    /// <exception cref="ForbiddenException">Thrown when the short URL exists but is no longer active or accessible.</exception>
    Task<UrlRedirectResult> GetRedirectInfoByShortCodeAsync(string shortCode, HttpContext context, CancellationToken cancellationToken = default);
    
    ClickTrackingData ExtractTrackingDataAsync(HttpContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Increments the click count for the specified short code.
    /// </summary>
    /// <param name="shortCode">The short code identifying the shortened URL.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// <c>true</c> if the click count was successfully incremented; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="shortCode"/> is null, empty, or whitespace.</exception>
    Task<bool> IncrementClickCountAsync(string shortCode, CancellationToken cancellationToken = default);

 
    /// <summary>
    /// Determines whether the click limit has been reached for a short URL.
    /// </summary>
    /// <param name="shortUrlId">The ID of the shortened URL.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// <c>true</c> if the click limit is reached; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> IsClickLimitReachedAsync(long shortUrlId, CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Determines whether a short URL is password-protected.
    /// </summary>
    /// <param name="shortUrlId">The ID of the shortened URL.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// <c>true</c> if the short URL requires a password; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> IsPasswordProtectedAsync(long shortUrlId, CancellationToken cancellationToken = default);

   
    /// <summary>
    /// Verifies whether the provided password is correct for the given short URL.
    /// </summary>
    /// <param name="shortUrlId">The ID of the shortened URL.</param>
    /// <param name="password">The plaintext password to verify.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// <c>true</c> if the password matches; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="password"/> is null, empty, or whitespace.</exception>
    Task<bool> VerifyPasswordAsync(long shortUrlId, string password, CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Retrieves the original URL if the provided password is correct for the given short code.
    /// </summary>
    /// <param name="shortCode">The short code identifying the shortened URL.</param>
    /// <param name="password">The plaintext password to verify.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// The original URL if the password is correct; otherwise, <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="shortCode"/> or <paramref name="password"/> is null, empty, or whitespace.
    /// </exception>
    Task<string?> GetUrlIfPasswordCorrectAsync(string shortCode, string password, CancellationToken cancellationToken = default);
    
   
    /// <summary>
    /// Determines whether the short URL is valid at the specified time.
    /// </summary>
    /// <param name="shortUrlId">The ID of the shortened URL.</param>
    /// <param name="nowUtc">The current UTC time used for validation.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// <c>true</c> if the URL is valid; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> IsValidAsync(long shortUrlId, DateTime nowUtc, CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Determines whether the short URL is currently active.
    /// </summary>
    /// <param name="shortUrlId">The ID of the shortened URL.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// <c>true</c> if the URL is active; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> IsActiveAsync(long shortUrlId, CancellationToken cancellationToken = default);
}
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ServerErrors;

namespace Shortly.Core.RepositoryContract.UrlManagement;

public interface IShortUrlRedirectRepository
{
    /// <summary>
    /// Retrieves redirect information for a short code with minimal data transfer.
    /// This method is optimized for high-frequency redirect operations.
    /// </summary>
    /// <param name="shortCode">The short code to get redirect information for.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// redirect information DTO if found, otherwise null.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when shortCode is null or empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method uses projection to minimize data transfer and includes
    /// expiration checking for active URLs only.
    /// </remarks>
    Task<ShortUrlRedirectInfoDto?> GetRedirectInfoByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Atomically increments the click count for a short URL and updates the timestamp.
    /// This operation is thread-safe and optimized for high concurrency.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to increment.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the increment was successful.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when shortCode is null or empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Uses ExecuteUpdateAsync for optimal performance and avoids race conditions
    /// in high-traffic scenarios. Only increments active, non-expired URLs.
    /// </remarks>
    Task<bool> IncrementClickCountAsync(string shortCode, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Checks if a short code already exists and is not expired.
    /// </summary>
    /// <param name="shortCode">The short code to check for existence.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the short code exists and is valid.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when shortCode is null or empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method is optimized for short code generation validation and only
    /// considers non-expired URLs as existing.
    /// </remarks>
    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);
  
    
    /// <summary>
    /// Checks if a short URL has reached its click limit.
    /// </summary>
    /// <param name="shortUrlId">The ID of the short URL to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the click limit has been reached.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// A click limit of -1 indicates unlimited clicks. The method returns false
    /// if the URL doesn't exist.
    /// </remarks>
    Task<bool> IsClickLimitReachedAsync(long shortUrlId, CancellationToken cancellationToken = default);
  
    
    /// <summary>
    /// Checks if a short URL is password protected.
    /// </summary>
    /// <param name="shortUrlId">The ID of the short URL to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the URL is password protected.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> IsPasswordProtectedAsync(long shortUrlId, CancellationToken cancellationToken = default);
  
    
    /// <summary>
    /// Verifies if the provided password hash matches the stored hash for a short URL.
    /// </summary>
    /// <param name="shortUrlId">The ID of the short URL.</param>
    /// <param name="passwordHash">The password hash to verify.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the password is correct.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when passwordHash is null or empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method assumes the password is already hashed before being passed in.
    /// It performs a secure comparison without retrieving the actual password hash.
    /// </remarks>
    Task<bool> VerifyPasswordAsync(long shortUrlId, string passwordHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the original URL associated with a given short code if the provided password hash matches.
    /// </summary>
    /// <param name="shortCode"> The unique short code that identifies the shortened URL.</param>
    /// <param name="passwordHash"> The hashed password to verify access to the shortened URL.</param>
    /// <param name="cancellationToken"> A token to monitor for cancellation requests.</param>
    /// <returns> The original URL if the short code and password hash match; otherwise, <c>null</c>.</returns>
    /// <exception cref="ArgumentException"> Thrown when <paramref name="shortCode"/> or <paramref name="passwordHash"/> is null, empty, or whitespace. </exception>
    /// <exception cref="DatabaseException"> Thrown when a database error occurs while verifying the password. </exception>
    /// <remarks>
    /// This method performs a case-sensitive comparison on both the short code and the password hash.
    /// It uses AsNoTracking to improve performance since no entity tracking is required.
    /// </remarks>
    Task<string?> GetUrlIfPasswordCorrectAsync(string shortCode, string passwordHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a short URL is valid (exists and not expired) based on its expiration date.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the short URL to check.</param>
    /// <param name="nowUtc">The current UTC date and time for comparison.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains:
    /// <c>true</c> if the URL is valid (exists and either has no expiration date or expires in the future);
    /// <c>false</c> if the URL doesn't exist or has an expiration date in the past.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during the validation check.</exception>
    /// <remarks>
    /// This method checks if a short URL exists and is not expired. A URL is considered valid if:
    /// - The URL exists in the database with the specified ID
    /// - The URL either has no expiration date (ExpiresAt is null) OR has an expiration date in the future
    /// 
    /// This method uses AsNoTracking() for optimal read-only performance.
    /// </remarks>
    Task<bool> IsValidAsync(long shortUrlId, DateTime nowUtc, CancellationToken cancellationToken = default);


    /// <summary>
    /// Determines whether a short URL is active and not expired.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the short URL to check.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation if needed.</param>
    /// <returns>
    /// <c>true</c> if the URL is active and not expired (exists, IsActive is true, and either has no expiration or expires in the future);
    /// <c>false</c> if the URL is inactive, expired, or doesn't exist.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during the active status check.</exception>
    /// <remarks>
    /// This method performs a comprehensive check to determine if a short URL is currently usable. A URL is considered active if:
    /// - The URL exists in the database with the specified ID
    /// - The URL's IsActive flag is set to true
    /// - The URL either has no expiration date (ExpiresAt is null) OR has an expiration date in the future
    /// 
    /// This method uses AsNoTracking() for optimal read-only performance.
    /// </remarks>
    Task<bool> IsActiveAsync(long shortUrlId, CancellationToken cancellationToken = default);
}
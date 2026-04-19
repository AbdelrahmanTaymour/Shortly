using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.ShortUrls.DTOs;

namespace Shortly.Core.ShortUrls.Contracts;

public interface IShortUrlRedirectQueries
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
}
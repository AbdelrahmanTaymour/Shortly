using System.ComponentModel.DataAnnotations;
using Shortly.Core.DTOs;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UrlManagement;

public interface IUrlBulkOperationsRepository
{
    /// <summary>
    /// Performs bulk deletion of short URLs by their IDs.
    /// </summary>
    /// <param name="ids">Collection of short URL IDs to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the bulk operation result with success and failure counts.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when ids collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when ids collection is empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This operation is atomic and optimized for performance. It logs the operation
    /// for audit purposes.
    /// </remarks>
    Task<BulkOperationResult> BulkDeleteAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Performs bulk deactivation of short URLs by their IDs.
    /// </summary>
    /// <param name="ids">Collection of short URL IDs to deactivate.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the bulk operation result with success and failure counts.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when ids collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when ids collection is empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<BulkOperationResult> BulkDeactivateAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Performs bulk activation of short URLs by their IDs.
    /// </summary>
    /// <param name="ids">Collection of short URL IDs to activate.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the bulk operation result with success and failure counts.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when ids collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when ids collection is empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<BulkOperationResult> BulkActivateAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Deletes all expired short URLs in a single operation.
    /// </summary>
    /// <param name="nowUtc">The current UTC time to compare expiration against.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the bulk operation result indicating how many URLs were deleted.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method is typically used by background services for cleanup operations.
    /// It only deletes URLs where ExpiresAt is not null and is less than or equal to nowUtc.
    /// </remarks>
    Task<BulkOperationResult> DeleteExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Updates the expiration date for multiple short URLs.
    /// </summary>
    /// <param name="ids">Collection of short URL IDs to update.</param>
    /// <param name="newExpirationDate">The new expiration date.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the bulk operation result.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when ids collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when ids collection is empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<BulkOperationResult> BulkUpdateExpirationAsync(IReadOnlyCollection<long> ids, DateTime? newExpirationDate, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Creates multiple short URLs in a single transaction for improved performance.
    /// </summary>
    /// <param name="shortUrls">Collection of short URLs to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the bulk operation result with creation statistics.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when shortUrls collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when shortUrls collection is empty.</exception>
    /// <exception cref="ValidationException">Thrown when any URL fails validation.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<BulkOperationResult> BulkCreateAsync(IReadOnlyCollection<ShortUrl> shortUrls, CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Performs a bulk update of short codes for the specified short URLs using a dictionary mapping.
    /// Updates both the ShortCode and UpdatedAt properties in a single database operation.
    /// </summary>
    /// <param name="shortUrlsMap">Dictionary mapping short URL IDs to their new short codes. Values can be null to clear the short code.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>
    /// A <see cref="BulkOperationResult"/> containing the total processed, success count, and failure count.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Uses ExecuteUpdateAsync for optimal performance. Non-existent IDs are counted as failures.
    /// UpdatedAt is automatically set to current UTC time. Null short code values will clear the existing short code.
    /// </remarks>
    Task<BulkOperationResult> BulkUpdateShortCodeAsync(IReadOnlyDictionary<long, string?> shortUrlsMap,
        CancellationToken cancellationToken = default);


    Task<HashSet<string?>> GetExistingCustomShortCodesAsync(IReadOnlyCollection<string> customCodes,
        CancellationToken cancellationToken);
}
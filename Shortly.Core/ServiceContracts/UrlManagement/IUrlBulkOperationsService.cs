using Shortly.Core.DTOs;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.ServiceContracts.Authentication;

namespace Shortly.Core.ServiceContracts.UrlManagement;

/// <summary>
/// Defins bulk operations for URL shortening services including creation, updates, and deletion.
/// Handles batch processing of multiple URLs with conflict resolution and validation.
/// </summary>
public interface IUrlBulkOperationsService
{
    /// <summary>
    /// Creates multiple short URLs in bulk, handling custom code conflicts and auto-generating codes as needed.
    /// </summary>
    /// <param name="shortUrlRequests">Collection of URL creation requests to process.</param>
    /// <param name="authContext">Authentication context with user and organization information.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>
    /// A <see cref="BulkCreateShortUrlResult"/> containing success count, conflicts, and failure details.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="shortUrlRequests"/> is null or empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Validates custom short codes for conflicts before creation. URLs with conflicting custom codes are skipped.
    /// Auto-generates short codes for URLs without custom codes using the database ID.
    /// </remarks>
    Task<BulkCreateShortUrlResult> BulkCreateAsync(IReadOnlyCollection<CreateShortUrlRequest> shortUrlRequests,
        IAuthenticationContext authContext, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Updates the expiration date for multiple short URLs in a single operation.
    /// </summary>
    /// <param name="ids">Collection of short URL IDs to update.</param>
    /// <param name="newExpirationDate">New expiration date to set, or null to remove expiration.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>A <see cref="BulkOperationResult"/> containing the operation results.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ids"/> is null or empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<BulkOperationResult> BulkUpdateExpirationAsync(IReadOnlyCollection<long> ids, DateTime? newExpirationDate, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Deletes multiple short URLs in a single operation.
    /// </summary>
    /// <param name="ids">Collection of short URL IDs to delete.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>A <see cref="BulkOperationResult"/> containing the operation results.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ids"/> is null or empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<BulkOperationResult> BulkDeleteAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Deletes all short URLs that have expired before the specified date.
    /// </summary>
    /// <param name="nowUtc">The current UTC time to compare against expiration dates.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>A <see cref="BulkOperationResult"/> containing the number of expired URLs deleted.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<BulkOperationResult> DeleteExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Activates multiple short URLs in a single operation, making them available for use.
    /// </summary>
    /// <param name="ids">Collection of short URL IDs to activate.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>A <see cref="BulkOperationResult"/> containing the operation results.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ids"/> is null or empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<BulkOperationResult> BulkActivateAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Deactivates multiple short URLs in a single operation, making them unavailable for use.
    /// </summary>
    /// <param name="ids">Collection of short URL IDs to deactivate.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>A <see cref="BulkOperationResult"/> containing the operation results.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ids"/> is null or empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<BulkOperationResult> BulkDeactivateAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
}
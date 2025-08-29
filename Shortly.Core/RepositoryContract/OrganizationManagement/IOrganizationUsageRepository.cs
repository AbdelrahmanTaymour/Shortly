using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Models;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

/// <summary>
///     Repository contract for managing organization-level usage statistics.
///     Provides methods for tracking and updating resource consumption, including links and QR codes, with
///     concurrency-safe operations.
/// </summary>
public interface IOrganizationUsageRepository
{
    /// <summary>
    ///     Retrieves usage statistics for a specific organization.
    ///     This method is functionally equivalent to <see cref="GetByOrganizationIdAsync" /> but provides semantic clarity for usage
    ///     reporting scenarios.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization whose usage data to retrieve.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     An instance of <see cref="OrganizationUsage"/> containing the usage data for the specified organization,
    ///     or <c>null</c> if no data exists for the given organization ID.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown if a database operation fails unexpectedly.</exception>
    /// <remarks>
    ///     This method uses <see cref="AsNoTracking"/> for optimal read performance, suitable for scenarios where 
    ///     data is not updated after retrieval.
    /// </remarks>
    Task<OrganizationUsage?> GetUsageStatsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new usage entry for a specific organization in the usage tracking system.
    /// This method is designed to initialize usage metrics for an organization.
    /// </summary>
    /// <param name="entity">The <see cref="OrganizationUsage"/> entity representing the organization's usage data to be created.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// An instance of <see cref="OrganizationUsage"/> containing the created usage data,
    /// or <c>null</c> if the operation fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="entity"/> is null.</exception>
    /// <exception cref="DatabaseException">Thrown if a database operation fails unexpectedly.</exception>
    /// <remarks>
    /// Ensure the <paramref name="entity"/> contains valid and initialized data
    /// before invoking this method to avoid integrity issues.
    /// </remarks>
    Task<OrganizationUsage?> CreateOrganizationUsageAsync(OrganizationUsage entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Safely increments the link creation statistics for a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to update.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if the increment succeeds within usage limits; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="DatabaseException">
    ///     Thrown if a concurrency conflict occurs or if the database operation fails unexpectedly.
    /// </exception>
    /// <remarks>
    ///     This method internally uses <see cref="ExecuteUpdateAsync"/> to ensure thread-safe and optimized increment
    ///     operations, preventing race conditions.
    /// </remarks>
    Task<bool> IncrementLinksCreatedAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Safely increments the QR code creation statistics for a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to update.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if the increment succeeds within usage limits; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="DatabaseException">
    ///     Thrown if a concurrency conflict occurs or if the database operation fails unexpectedly.
    /// </exception>
    /// <remarks>
    ///     This method internally uses <see cref="ExecuteUpdateAsync"/> to ensure thread-safe and optimized increment
    ///     operations, preventing race conditions.
    /// </remarks>
    Task<bool> IncrementQrCodesCreatedAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the subscription limits and status information for a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to retrieve limits for.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     An <see cref="OrganizationLimits"/> object containing the organization's subscription limits and status, 
    ///     or <c>null</c> if the organization is not found.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown if the database operation fails unexpectedly.</exception>
    /// <remarks>
    ///     This method uses a read-only query with minimal data projection for optimal performance. 
    ///     The returned limits include subscription plan constraints and organization status flags.
    /// </remarks>
    Task<OrganizationLimits?> GetOrganizationLimitsAsync(Guid organizationId, CancellationToken cancellationToken = default);
   
    /// <summary>
    ///     Checks whether an organization can create additional links based on their current monthly usage and subscription limits.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to check.</param>
    /// <param name="maxLinksPerMonth">The maximum number of links allowed per month for the organization's subscription plan.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if the organization can create more links within their monthly limit; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown if the database operation fails unexpectedly.</exception>
    /// <remarks>
    ///     This method performs an efficient database query to check current usage against the provided limit.
    ///     It does not perform automatic monthly usage reset.
    /// </remarks>
    Task<bool> CanCreateMoreLinksAsync(Guid organizationId, int maxLinksPerMonth, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Checks whether an organization can create additional QR codes based on their current monthly usage and subscription limits.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to check.</param>
    /// <param name="maxQrCodesPerMonth">The maximum number of QR codes allowed per month for the organization's subscription plan.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if the organization can create more QR codes within their monthly limit; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown if the database operation fails unexpectedly.</exception>
    /// <remarks>
    ///     This method performs an efficient database query to check current usage against the provided limit.
    ///     It does not perform automatic monthly usage reset.
    /// </remarks>
    Task<bool> CanCreateMoreQrCodesAsync(Guid organizationId, int maxQrCodesPerMonth, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Resets the monthly usage counters (links and QR codes created) to zero for a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization whose usage needs resetting.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if the reset operation successfully updates the organization's usage; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown if the database operation fails unexpectedly.</exception>
    /// <remarks>
    ///     This method resets the organization's usage counters and updates the next monthly reset date. It uses
    ///     an efficient bulk update operation for performance.
    /// </remarks>
    Task<bool> ResetMonthlyUsageAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Performs a bulk reset of monthly usage statistics for all organizations whose reset date has passed.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The number of organization usage records that were successfully reset.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown if the database operation fails unexpectedly.</exception>
    /// <remarks>
    ///     This method efficiently resets monthly counters for multiple organizations in a single database operation.
    ///     It automatically sets the next monthly reset date to one month from the current UTC time.
    /// </remarks>
    Task<int> BulkResetMonthlyUsageAsync(CancellationToken cancellationToken = default);
}
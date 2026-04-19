using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.ShortUrls.DTOs;

namespace Shortly.Core.Analytics.Contracts;

public interface IShortUrlAnalyticsDapperQueries
{
    /// <summary>
    ///     Gets analytics summary for a user's short URLs.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     an analytics summary including total URLs, clicks, and other metrics.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<UserAnalyticsSummary> GetUserAnalyticsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets analytics summary for an organization's short URLs.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     analytics summary for the organization.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<OrganizationAnalyticsSummary> GetOrganizationAnalyticsAsync(Guid organizationId,
        CancellationToken cancellationToken = default);
}
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UrlManagement;

public interface IShortUrlAnalyticsRepository
{
    /// <summary>
    ///     Gets the total number of short URLs in the system.
    /// </summary>
    /// <param name="activeOnly">Whether to count only active URLs.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     the total count of short URLs.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<int> GetTotalCountAsync(bool activeOnly = false, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Gets the total click count for a specific short URL.
    /// </summary>
    /// <param name="shortUrlId">The ID of the short URL.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     the total number of clicks for the URL.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<int> GetTotalClicksAsync(long shortUrlId, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Retrieves the most popular short URLs based on click count.
    /// </summary>
    /// <param name="topCount">Number of top URLs to retrieve (1-100).</param>
    /// <param name="timeframe">Optional timeframe to filter by creation date.</param>
    /// <param name="userId">Optional user ID to filter by owner.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     a read-only list of the most popular short URLs.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when topCount is invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<IEnumerable<ShortUrl>> GetMostPopularUrlAsync(int topCount = 10, TimeSpan? timeframe = null,
        Guid? userId = null, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Gets analytics summary for a user's short URLs.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     analytics summary including total URLs, clicks, and other metrics.
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

    /// <summary>
    ///     Retrieves short URLs that are approaching their click limits.
    /// </summary>
    /// <param name="warningThreshold">Percentage threshold (0.0-1.0) for warning.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     short URLs that have reached the warning threshold of their click limit.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when warningThreshold is not between 0 and 1.</exception>
    /// <exception cref="ValidationException">Thrown when pagination parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    ///     For example, warningThreshold of 0.8 will return URLs that have used 80% or more
    ///     of their click limit. URLs with unlimited clicks (ClickLimit = -1) are excluded.
    /// </remarks>
    Task<IEnumerable<ShortUrl>> GetApproachingLimitAsync(double warningThreshold = 0.8, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default);
}
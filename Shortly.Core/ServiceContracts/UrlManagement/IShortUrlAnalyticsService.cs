using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;

namespace Shortly.Core.ServiceContracts.UrlManagement;

/// <summary>
/// Defins analytics operations for short URLs, including statistics retrieval,
/// popularity rankings, and usage limit monitoring.
/// </summary>
public interface IShortUrlAnalyticsService
{
    /// <summary>
    /// Retrieves the total number of short URLs in the system.
    /// </summary>
    /// <param name="activeOnly">
    /// If <see langword="true"/>, counts only active URLs; otherwise counts all URLs.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The total number of short URLs matching the criteria.</returns>
    /// <exception cref="DatabaseException">
    /// Thrown when there is an error querying the database.
    /// </exception>
    Task<int> GetTotalCountAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Retrieves the total click count for a specific short URL.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the short URL.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The total number of recorded clicks for the specified URL.</returns>
    /// <exception cref="DatabaseException">
    /// Thrown when there is an error querying the database.
    /// </exception>
    Task<int> GetTotalClicksAsync(long shortUrlId, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Retrieves the most popular short URLs ranked by click count.
    /// </summary>
    /// <param name="topCount">The maximum number of URLs to return (1 to 100).</param>
    /// <param name="timeframe">
    /// Optional time range; if specified, only URLs created within this timeframe are considered.
    /// </param>
    /// <param name="userId">
    /// Optional user ID filter; if specified, only URLs owned by this user are considered.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A collection of the most popular short URLs as DTOs.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="topCount"/> is outside the range 1–100.
    /// </exception>
    /// <exception cref="DatabaseException">
    /// Thrown when there is an error querying the database.
    /// </exception>
    Task<IEnumerable<ShortUrlDto>> GetMostPopularUrlAsync(int topCount = 10, TimeSpan? timeframe = null,
        Guid? userId = null, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Retrieves aggregated analytics for a specific user, including URL counts and click statistics.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="UserAnalyticsSummary"/> containing the aggregated statistics.</returns>
    /// <exception cref="DatabaseException">
    /// Thrown when there is an error querying the database.
    /// </exception>
    Task<UserAnalyticsSummary> GetUserAnalyticsAsync(Guid userId, CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Retrieves aggregated analytics for a specific organization, including URL counts,
    /// click statistics, and member activity.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>An <see cref="OrganizationAnalyticsSummary"/> containing the aggregated statistics.</returns>
    /// <exception cref="DatabaseException">
    /// Thrown when there is an error querying the database.
    /// </exception>
    Task<OrganizationAnalyticsSummary> GetOrganizationAnalyticsAsync(Guid organizationId,
        CancellationToken cancellationToken = default);
    
   
    /// <summary>
    /// Retrieves short URLs that are approaching their configured click limit.
    /// </summary>
    /// <param name="warningThreshold">
    /// The fraction of the click limit at which to trigger a warning (0.0 to 1.0).
    /// Defaults to 0.8 (80% of the limit).
    /// </param>
    /// <param name="pageNumber">The page number to retrieve (starting from 1).</param>
    /// <param name="pageSize">The number of items per page (1 to 1000).</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A collection of short URLs nearing their click limits as DTOs.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="warningThreshold"/> is outside the range 0.0–1.0.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when <paramref name="pageNumber"/> or <paramref name="pageSize"/> is invalid.
    /// </exception>
    /// <exception cref="DatabaseException">
    /// Thrown when there is an error querying the database.
    /// </exception>
    Task<IEnumerable<ShortUrlDto>> GetApproachingLimitAsync(double warningThreshold = 0.8, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default);
}
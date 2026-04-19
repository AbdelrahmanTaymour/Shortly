using Shortly.Core.Analytics.Contracts;
using Shortly.Core.ShortUrls.DTOs;
using Shortly.Core.ShortUrls.Mappers;
using Shortly.Domain.RepositoryContract.Analytics;

namespace Shortly.Core.Analytics.Services;

/// <summary>
/// Provides analytics operations for short URLs, including statistics retrieval,
/// popularity rankings, and usage limit monitoring.
/// </summary>
public class ShortUrlAnalyticsService(
    IShortUrlAnalyticsRepository analyticsRepository,
    IShortUrlAnalyticsDapperQueries analyticsDapperQueries
    ) : IShortUrlAnalyticsService
{
    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        return await analyticsRepository.GetTotalCountAsync(activeOnly, cancellationToken);
    }

    
    /// <inheritdoc />
    public async Task<int> GetTotalClicksAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        return await analyticsRepository.GetTotalClicksAsync(shortUrlId, cancellationToken);
    }
    
    
    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrlDto>> GetMostPopularUrlAsync(int topCount = 10, TimeSpan? timeframe = null, Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var urls = await analyticsRepository.GetMostPopularUrlAsync(topCount, timeframe, userId, cancellationToken);
        return urls.MapToShortUrlDtos();
    }

    
    /// <inheritdoc />
    public async Task<UserAnalyticsSummary> GetUserAnalyticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await analyticsDapperQueries.GetUserAnalyticsAsync(userId, cancellationToken);
    }
   
    /// <inheritdoc />
    public async Task<OrganizationAnalyticsSummary> GetOrganizationAnalyticsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await analyticsDapperQueries.GetOrganizationAnalyticsAsync(organizationId, cancellationToken);
    }

    
    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrlDto>> GetApproachingLimitAsync(
        double warningThreshold = 0.8, 
        int pageNumber = 1, 
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var urls = await analyticsRepository.GetApproachingLimitAsync(warningThreshold, pageNumber, pageSize,
            cancellationToken);
        return urls.MapToShortUrlDtos();
    }
}
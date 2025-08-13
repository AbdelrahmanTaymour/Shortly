using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.UrlManagement;
using Shortly.Core.ServiceContracts.UrlManagement;

namespace Shortly.Core.Services.UrlManagement;

/// <summary>
/// Provides analytics operations for short URLs, including statistics retrieval,
/// popularity rankings, and usage limit monitoring.
/// </summary>
public class ShortUrlAnalyticsService(IShortUrlAnalyticsRepository analyticsRepository) : IShortUrlAnalyticsService
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
        return await analyticsRepository.GetUserAnalyticsAsync(userId, cancellationToken);
    }
   
    /// <inheritdoc />
    public async Task<OrganizationAnalyticsSummary> GetOrganizationAnalyticsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await analyticsRepository.GetOrganizationAnalyticsAsync(organizationId, cancellationToken);
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
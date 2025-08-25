using Microsoft.Extensions.Logging;
using Shortly.Core.Models;
using Shortly.Core.RepositoryContract.ClickTracking;
using Shortly.Core.ServiceContracts.ClickTracking;
using Shortly.Core.ServiceContracts.UrlManagement;
using Shortly.Domain.Entities;

namespace Shortly.Core.Services.ClickTracking;

/// <summary>
/// Service responsible for tracking, analyzing, and managing click events for shortened URLs.
/// Provides comprehensive click analytics including geographical, device, browser, and traffic source analysis.
/// </summary>
/// <remarks>
/// This service orchestrates multiple components to provide complete click tracking functionality:
/// - Click Event Creation: Records detailed click information with enriched metadata
/// - Analytics Generation: Provides comprehensive analytics and reporting capabilities
/// - Data Enrichment: Enhances click data with geolocation, user agent parsing, and traffic source analysis
/// - Data Management: Handles click history retrieval and cleanup operations
/// 
/// The service integrates with:
/// - Geolocation services for country/city identification
/// - User agent parsing for browser/device/OS detection
/// - Traffic source analysis for marketing attribution
/// - Repository layer for data persistence and retrieval
/// 
/// All operations are designed to be asynchronous and support cancellation for optimal performance
/// and resource management in high-traffic scenarios.
/// </remarks>
/// <param name="shortUrlAnalyticsService">Service for URL-specific analytics operations</param>
/// <param name="clickEventRepository">Repository for click event data persistence and retrieval</param>
/// <param name="geoLocationService">Service for IP address geolocation lookup</param>
/// <param name="userAgentParsingService">Service for parsing User-Agent strings</param>
/// <param name="trafficSourceAnalyzer">Service for analyzing traffic source attribution</param>
/// <param name="logger">Logger instance for operation tracking and error reporting</param>
public class ClickTrackingService (
    IShortUrlAnalyticsService shortUrlAnalyticsService,
    IClickEventRepository clickEventRepository, 
    IGeoLocationService geoLocationService,
    IUserAgentParsingService userAgentParsingService,
    ITrafficSourceAnalyzer trafficSourceAnalyzer,
    ILogger<ClickTrackingService> logger
    ) : IClickTrackingService
{
    
    /// <inheritdoc />
    public async Task<ClickEvent> TrackClickAsync(long shortUrlId, ClickTrackingData trackingData)
    {
        try
        {
            var userAgentInfo = userAgentParsingService.ParseUserAgent(trackingData.UserAgent);
            var geoInfo = await geoLocationService.GetLocationInfoAsync(trackingData.IpAddress);

            var trafficSourceInfo = trafficSourceAnalyzer.AnalyzeTrafficSource(trackingData.Referrer,
                trackingData.UtmSource, trackingData.UtmMedium);
            var clickEvent = new ClickEvent
            {
                ShortUrlId = shortUrlId,
                ClickedAt = DateTime.UtcNow,
                IpAddress = trackingData.IpAddress,
                SessionId = trackingData.SessionId,
                UserAgent = trackingData.UserAgent,
                Referrer = trackingData.Referrer,
                UtmSource = trackingData.UtmSource,
                UtmMedium = trackingData.UtmMedium,
                UtmCampaign = trackingData.UtmCampaign,
                UtmTerm = trackingData.UtmTerm,
                UtmContent = trackingData.UtmContent,
                Country = geoInfo.Country,
                City = geoInfo.City,
                Browser = userAgentInfo.Browser,
                OperatingSystem = userAgentInfo.OperatingSystem,
                Device = userAgentInfo.Device,
                DeviceType = userAgentInfo.DeviceType,
                ReferrerDomain = trafficSourceInfo.ReferrerDomain,
                TrafficSource = trafficSourceInfo.TrafficSource
            };

            var savedClickEvent = await clickEventRepository.CreateAsync(clickEvent);
            logger.LogInformation("Click tracked successfully for ShortUrlId: {ShortUrlId}, ClickEventId: {ClickEventId}", shortUrlId, savedClickEvent.Id);
            return savedClickEvent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error tracking click for ShortUrlId: {ShortUrlId}", shortUrlId);
            throw;
        }
    }


    /// <inheritdoc />
    public async Task<ClickAnalytics> GetAnalyticsAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var analytics = new ClickAnalytics
        {
            ShortUrlId = shortUrlId,
            StartDate = startDate,
            EndDate = endDate
        };
        
        // Get total clicks
        if(startDate.HasValue && endDate.HasValue)
        {
            analytics.TotalClicks = await clickEventRepository.GetTotalClicksInDateRangeAsync(shortUrlId, startDate.Value, endDate.Value, cancellationToken);
        }
        else
        {
            analytics.TotalClicks = await clickEventRepository.GetTotalClicksAsync(shortUrlId, cancellationToken);
        }
        
        // Get breakdowns
        analytics.ClicksByCountry = await clickEventRepository.GetClicksByCountryAsync(shortUrlId, startDate, endDate, cancellationToken);
        analytics.ClicksByDeviceType = await clickEventRepository.GetClicksByDeviceTypeAsync(shortUrlId, startDate, endDate, cancellationToken);
        analytics.ClicksByTrafficSource = await clickEventRepository.GetClicksByTrafficSourceAsync(shortUrlId, startDate, endDate, cancellationToken);
        
        // Get time-based analytics if the date range is specified
        if (startDate.HasValue && endDate.HasValue)
        {
            analytics.DailyClicks = await clickEventRepository.GetDailyClicksAsync(shortUrlId, startDate.Value, endDate.Value, cancellationToken);
        }
        
        return analytics;
    }

    
    /// <inheritdoc />
    public async Task<ClickAnalytics> GetRealTimeAnalyticsAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        // For real-time analytics, get data for the last 24 hours
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-1);
        
        return await GetAnalyticsAsync(shortUrlId, startDate, endDate, cancellationToken);
    }
    
    
    /// <inheritdoc />
    public async Task<IEnumerable<ClickEvent>> GetRecentClicksAsync(long shortUrlId, int count = 10, CancellationToken cancellationToken = default)
    {
        return await clickEventRepository.GetRecentClicksAsync(shortUrlId, count, cancellationToken);
    }

    
    /// <inheritdoc />
    public async Task<PaginatedResult<ClickEvent>> GetClickHistoryAsync(long shortUrlId, int pageNumber = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var clicks = await clickEventRepository.GetByShortUrlIdAsync(shortUrlId, pageNumber, pageSize, cancellationToken);
        var totalCount = await shortUrlAnalyticsService.GetTotalClicksAsync(shortUrlId, cancellationToken);
        
        return new PaginatedResult<ClickEvent>
        {
            Items = clicks,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    
    /// <inheritdoc />
    public async Task<int> CleanupOldClicksAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - retentionPeriod;
        var deletedCount = await clickEventRepository.DeleteOldClicksAsync(cutoffDate, cancellationToken);
        logger.LogInformation("Cleaned up {DeletedCount} old clicks older than {CutoffDate}", deletedCount, cutoffDate);
        return deletedCount;
    }
}
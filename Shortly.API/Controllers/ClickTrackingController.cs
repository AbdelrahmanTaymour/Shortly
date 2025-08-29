using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.Models;
using Shortly.Core.ServiceContracts.ClickTracking;
using Shortly.Core.ServiceContracts.UrlManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/short-urls/tracking")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class ClickTrackingController(IClickTrackingService clickTrackingService, IShortUrlRedirectService shortUrlRedirectService) : ControllerApiBase
{
    /// <summary>
    /// Tracks a click event for a specific short URL and returns the recorded click data.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the short URL that was clicked.</param>
    /// <returns>The recorded click event with all tracking information.</returns>
    /// <example>POST /api/short-urls/tracking/123456789/click</example>
    /// <remarks>
    /// <para><strong>Access:</strong> [AllowAnonymous] This endpoint is accessible to everyone to enable click tracking for all users.</para>
    /// 
    /// Sample Request:
    ///
    ///     POST /api/short-urls/tracking/123456789/click
    ///
    /// This endpoint automatically captures and records comprehensive click analytics including:
    /// - **Geographic Information**: Country and city based on IP address
    /// - **Device Information**: Browser, operating system, device type
    /// - **Traffic Source**: Referrer domain and traffic source analysis
    /// - **UTM Parameters**: Campaign tracking parameters from query string
    /// - **Session Tracking**: Anonymous session identification
    /// - **Timestamp**: Precise click timing in UTC
    /// 
    /// **Automatic Data Collection:**
    /// The endpoint automatically extracts tracking data from the HTTP request:
    /// - IP address (with proxy support via X-Forwarded-For)
    /// - User agent string for device/browser detection
    /// - Referrer header for traffic source analysis
    /// - UTM parameters (utm_source, utm_medium, utm_campaign, utm_term, utm_content)
    /// - Session identification for anonymous user tracking
    /// 
    /// **Privacy Considerations:**
    /// - IP addresses are processed for geographic insights only
    /// - No personally identifiable information is stored
    /// - Complies with privacy regulations and best practices
    /// </remarks>
    /// <response code="200">Click event was tracked successfully. Returns the complete click event data.</response>
    /// <response code="400">The request is invalid (e.g., invalid shortUrlId).</response>
    /// <response code="404">Short URL with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred during click tracking.</response>
    [HttpPost("{shortUrlId:long}/click", Name = "TrackClick")]
    [ProducesResponseType(typeof(ClickEvent), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> TrackClick(long shortUrlId, CancellationToken cancellationToken = default)
    {
        var trackingData = shortUrlRedirectService.ExtractTrackingDataAsync(HttpContext, cancellationToken);
        var clickEvent = await clickTrackingService.TrackClickAsync(shortUrlId, trackingData);
        return Ok(clickEvent);
    }


    /// <summary>
    /// Retrieves comprehensive analytics for a specific short URL with optional date range filtering.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the short URL to get analytics for.</param>
    /// <param name="startDate">Optional. The start date for analytics filtering (inclusive). If not provided, includes all historical data.</param>
    /// <param name="endDate">Optional. The end date for analytics filtering (inclusive). If not provided, includes data up to the current date.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Comprehensive analytics including click counts, geographic breakdown, device information, and traffic sources.</returns>
    /// <example>GET /api/short-urls/tracking/123456789/analytics?startDate=2024-01-01T00:00:00Z&amp;endDate=2024-12-31T23:59:59Z</example>
    /// <remarks>
    /// Sample Requests:
    ///
    ///     GET /api/short-urls/tracking/123456789/analytics
    ///     GET /api/short-urls/tracking/123456789/analytics?startDate=2024-01-01T00:00:00Z
    ///     GET /api/short-urls/tracking/123456789/analytics?startDate=2024-01-01T00:00:00Z&amp;endDate=2024-12-31T23:59:59Z
    /// 
    /// This endpoint provides comprehensive analytics insights including:
    /// 
    /// **Core Metrics:**
    /// - Total click count for the specified period
    /// - Daily click trends (when date range is specified)
    /// 
    /// **Geographic Analytics:**
    /// - Clicks breakdown by country
    /// - Geographic distribution insights
    /// 
    /// **Device Analytics:**
    /// - Clicks by device type (desktop, mobile, tablet)
    /// - Browser and operating system statistics
    /// 
    /// **Traffic Source Analytics:**
    /// - Traffic source breakdown (direct, social, search, referral)
    /// - Referrer domain analysis
    /// - UTM campaign performance
    /// 
    /// **Time-Based Analytics:**
    /// - Daily click patterns (when date range is provided)
    /// - Trend analysis over time
    /// 
    /// Perfect for creating dashboards, generating reports, and understanding user engagement patterns.
    /// </remarks>
    /// <response code="200">Returns comprehensive analytics data for the short URL.</response>
    /// <response code="400">The request parameters are invalid (e.g., invalid date format, start date after end date).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view analytics for this URL.</response>
    /// <response code="404">Short URL with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{shortUrlId:long}/analytics", Name = "GetClickAnalytics")]
    [ProducesResponseType(typeof(ClickAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadOwnAnalytics | enPermissions.ReadOrgAnalytics)]
    public async Task<IActionResult> GetAnalytics(long shortUrlId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            return BadRequest("Start date must be before or equal to end date.");

        var analytics = await clickTrackingService.GetAnalyticsAsync(shortUrlId, startDate, endDate, cancellationToken);
        return Ok(analytics);
    }

    /// <summary>
    /// Retrieves real-time analytics for a specific short URL covering the last 24 hours.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the short URL to get real-time analytics for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Real-time analytics data for the last 24 hours including recent click patterns and trends.</returns>
    /// <example>GET /api/short-urls/tracking/123456789/real-time</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///     GET /api/short-urls/tracking/123456789/real-time
    /// 
    /// This endpoint provides live analytics insights for monitoring immediate URL performance.
    /// It automatically analyzes data from the last 24 hours to show:
    /// 
    /// **Real-Time Insights:**
    /// - Recent click activity and trends
    /// - Geographic distribution of recent clicks
    /// - Device and browser usage patterns
    /// - Traffic source analysis for recent visitors
    /// - Hourly click distribution
    /// 
    /// **Use Cases:**
    /// - Live monitoring during campaigns
    /// - Real-time performance dashboards
    /// - Immediate feedback on marketing efforts
    /// - Quick health checks for active URLs
    /// - Social media campaign monitoring
    /// 
    /// **Performance Optimized:**
    /// - Fixed 24-hour window for consistent response times
    /// - Efficient queries for real-time data requirements
    /// - Suitable for frequent polling and live updates
    /// 
    /// Perfect for real-time dashboards and monitoring active campaigns.
    /// </remarks>
    /// <response code="200">Returns real-time analytics data for the last 24 hours.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view real-time analytics for this URL.</response>
    /// <response code="404">Short URL with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{shortUrlId:long}/real-time", Name = "GetRealTimeAnalytics")]
    [ProducesResponseType(typeof(ClickAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewRealTimeStats)]
    public async Task<IActionResult> GetRealTimeAnalytics(long shortUrlId, CancellationToken cancellationToken = default)
    {
        var realTimeAnalytics = await clickTrackingService.GetRealTimeAnalyticsAsync(shortUrlId, cancellationToken);
        return Ok(realTimeAnalytics);
    }

    /// <summary>
    /// Retrieves the most recent click events for a specific short URL.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the short URL to get recent clicks for.</param>
    /// <param name="count">The number of recent clicks to retrieve. Default is 10, maximum is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of the most recent click events ordered by click time (most recent first).</returns>
    /// <example>GET /api/short-urls/tracking/123456789/recent-clicks?count=20</example>
    /// <remarks>
    /// Sample Requests:
    ///
    ///     GET /api/short-urls/tracking/123456789/recent-clicks
    ///     GET /api/short-urls/tracking/123456789/recent-clicks?count=20
    ///     GET /api/short-urls/tracking/123456789/recent-clicks?count=50
    /// 
    /// This endpoint provides immediate visibility into recent user activity and engagement.
    /// Each click event contains detailed information including:
    /// 
    /// **Click Event Details:**
    /// - Timestamp of the click (UTC)
    /// - Geographic location (country, city)
    /// - Device information (browser, OS, device type)
    /// - Traffic source and referrer information
    /// - UTM campaign parameters
    /// - Session and user agent details
    /// 
    /// **Use Cases:**
    /// - Monitoring recent activity and engagement
    /// - Debugging and troubleshooting click issues
    /// - Understanding user behavior patterns
    /// - Real-time activity feeds and notifications
    /// - Quality assurance for tracking implementation
    /// 
    /// **Performance Considerations:**
    /// - Results are ordered by most recent clicks first
    /// - Limited to 100 items maximum for performance
    /// - Efficient queries for quick response times
    /// - Suitable for frequent polling and live updates
    /// 
    /// Perfect for activity monitoring and real-time user engagement insights.
    /// </remarks>
    /// <response code="200">Returns the list of recent click events.</response>
    /// <response code="400">The count parameter is invalid (e.g., negative value or exceeds maximum limit).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view click details for this URL.</response>
    /// <response code="404">Short URL with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{shortUrlId:long}/recent-clicks", Name = "GetRecentClicks")]
    [ProducesResponseType(typeof(IEnumerable<ClickEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadOwnAnalytics | enPermissions.ReadTeamAnalytics | enPermissions.ReadOrgAnalytics | enPermissions.ViewRealTimeStats)]
    public async Task<IActionResult> GetRecentClicks(long shortUrlId, [FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 100)
            return BadRequest("Count must be between 1 and 100.");

        var recentClicks = await clickTrackingService.GetRecentClicksAsync(shortUrlId, count, cancellationToken);
        return Ok(recentClicks);
    }

    /// <summary>
    /// Retrieves paginated click history for a specific short URL.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the short URL to get click history for.</param>
    /// <param name="pageNumber">The page number to retrieve (starting from 1). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, maximum is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated result containing click events and pagination metadata.</returns>
    /// <example>GET /api/short-urls/tracking/123456789/click-history?pageNumber=1&amp;pageSize=25</example>
    /// <remarks>
    /// Sample Requests:
    ///
    ///     GET /api/short-urls/tracking/123456789/click-history
    ///     GET /api/short-urls/tracking/123456789/click-history?pageNumber=2
    ///     GET /api/short-urls/tracking/123456789/click-history?pageNumber=1&amp;pageSize=25
    ///     GET /api/short-urls/tracking/123456789/click-history?pageNumber=3&amp;pageSize=100
    /// 
    /// This endpoint provides comprehensive access to all historical click data with efficient pagination.
    /// The response includes both the click events and pagination metadata:
    /// 
    /// **Response Structure:**
    /// ```json
    /// {
    ///   "items": [...], // Array of ClickEvent objects
    ///   "totalCount": 1250, // Total number of clicks
    ///   "pageNumber": 1, // Current page number
    ///   "pageSize": 50, // Items per page
    ///   "totalPages": 25, // Total number of pages
    ///   "hasNextPage": true, // Whether next page exists
    ///   "hasPreviousPage": false // Whether previous page exists
    /// }
    /// ```
    /// 
    /// **Use Cases:**
    /// - Historical data analysis and reporting
    /// - Detailed audit trails and compliance
    /// - Data export and backup operations
    /// - Comprehensive analytics and insights
    /// - User activity investigation and debugging
    /// 
    /// **Performance Optimized:**
    /// - Efficient pagination to handle large datasets
    /// - Ordered by click timestamp (most recent first)
    /// - Optimized queries for fast response times
    /// - Memory-efficient processing for large result sets
    /// 
    /// Perfect for comprehensive data analysis and historical reporting.
    /// </remarks>
    /// <response code="200">Returns the paginated click history with metadata.</response>
    /// <response code="400">The pagination parameters are invalid (e.g., invalid page numbers or page size).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view click history for this URL.</response>
    /// <response code="404">Short URL with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{shortUrlId:long}/click-history", Name = "GetClickHistory")]
    [ProducesResponseType(typeof(PaginatedResult<ClickEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewHistoricalData)]
    public async Task<IActionResult> GetClickHistory(long shortUrlId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            return BadRequest("Page number must be greater than 0.");
        
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100.");

        var clickHistory = await clickTrackingService.GetClickHistoryAsync(shortUrlId, pageNumber, pageSize, cancellationToken);
        return Ok(clickHistory);
    }

    /// <summary>
    /// Performs cleanup of old click tracking data based on a retention period.
    /// </summary>
    /// <param name="retentionDays">The number of days to retain click data. Data older than this will be permanently deleted. Must be a positive value.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of click events that were deleted during the cleanup operation.</returns>
    /// <example>DELETE /api/short-urls/tracking/cleanup?retentionDays=365</example>
    /// <remarks>
    /// Sample Requests:
    ///
    ///     DELETE /api/short-urls/tracking/cleanup?retentionDays=365
    ///     DELETE /api/short-urls/tracking/cleanup?retentionDays=180
    ///     DELETE /api/short-urls/tracking/cleanup?retentionDays=90
    /// 
    /// **⚠️ WARNING: This operation permanently deletes old click data and cannot be undone.**
    /// 
    /// This endpoint performs system-wide cleanup of historical click tracking data to:
    /// - **Comply with Data Retention Policies**: Meet regulatory requirements for data retention
    /// - **Optimize Database Performance**: Remove old data to improve query performance
    /// - **Manage Storage Costs**: Reduce database storage requirements
    /// - **Maintain System Health**: Prevent database bloat from excessive historical data
    /// 
    /// **Administrative Operation:**
    /// This is a system-level maintenance operation that affects click data across all URLs and users.
    /// Only users with appropriate administrative permissions should have access to this endpoint.
    /// 
    /// **Best Practices:**
    /// - Run during low-traffic periods to minimize system impact
    /// - Consider backing up data before cleanup if needed for compliance
    /// - Monitor the number of deleted records to ensure expected behavior
    /// - Schedule regular cleanup operations based on your retention policies
    /// 
    /// **Common Retention Periods:**
    /// - **GDPR Compliance**: Typically 12-24 months
    /// - **Analytics Needs**: 6-18 months for trend analysis
    /// - **Storage Optimization**: 3-12 months depending on volume
    /// - **Audit Requirements**: Varies by industry and regulations
    /// </remarks>
    /// <response code="200">Cleanup completed successfully. Returns the number of deleted click events.</response>
    /// <response code="400">The retentionDays parameter is invalid (e.g., negative or zero value).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to perform system cleanup operations.</response>
    /// <response code="500">An internal server error occurred during cleanup processing.</response>
    [HttpDelete("cleanup", Name = "CleanupOldClicks")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.CleanupSystemData)]
    public async Task<IActionResult> CleanupOldClicks([FromQuery] int retentionDays, CancellationToken cancellationToken = default)
    {
        if (retentionDays <= 0)
            return BadRequest("retentionDays must be a positive value.");

        var retentionPeriod = TimeSpan.FromDays(retentionDays);
        var deletedCount = await clickTrackingService.CleanupOldClicksAsync(retentionPeriod, cancellationToken);
        return Ok(deletedCount);
    }
}
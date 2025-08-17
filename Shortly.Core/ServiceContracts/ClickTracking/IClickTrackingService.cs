using Microsoft.AspNetCore.Http;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Models;
using Shortly.Domain.Entities;

namespace Shortly.Core.ServiceContracts.ClickTracking;

public interface IClickTrackingService
{
    /// <summary>
    /// Tracks a click event for a shortened URL with comprehensive data enrichment and persistence.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the shortened URL that was clicked</param>
    /// <param name="trackingData"></param>
    /// <returns>
    /// The created and persisted <see cref="ClickEvent"/> with all enriched data populated
    /// </returns>
    /// <remarks>
    /// This method performs comprehensive click tracking with the following enrichment steps:
    /// 
    /// 1. User Agent Analysis:
    /// - Extracts browser name and version
    /// - Identifies operating system and version  
    /// - Determines device type and model
    /// 
    /// 2. Geolocation Processing:
    /// - Resolves IP address to country and city
    /// - Handles both IPv4 and IPv6 addresses
    /// - Provides fallback for unknown locations
    /// 
    /// 3. Traffic Source Attribution:
    /// - Analyzes UTM parameters for campaign tracking
    /// - Processes referrer data for organic traffic identification
    /// - Categorizes traffic sources (Search, Social, Direct, etc.)
    /// 
    /// 4. Data Persistence:
    /// - Creates complete ClickEvent entity with all metadata
    /// - Persists to a database with transaction safety
    /// - Returns the saved entity with generated identifiers
    /// 
    /// The method is designed to handle high-volume traffic and provides detailed logging
    /// for monitoring and debugging purposes. All external service calls are made
    /// asynchronously to maintain performance.
    /// </remarks>
    /// <example>
    /// <code>
    /// var trackingData = new ClickTrackingData
    /// {
    ///     IpAddress = "192.168.1.1",
    ///     UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36...",
    ///     Referrer = "https://google.com/search",
    ///     UtmSource = "google",
    ///     UtmMedium = "cpc",
    ///     SessionId = "session-123"
    /// };
    /// 
    /// var clickEvent = await service.TrackClickAsync(12345, trackingData);
    /// // Returns enriched click event with geolocation, browser info, and traffic source
    /// </code>
    /// </example>
    Task<ClickEvent> TrackClickAsync(long shortUrlId, ClickTrackingData trackingData);
    
    
    /// <summary>
    /// Generates comprehensive analytics for a shortened URL, optionally filtered by date range.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the shortened URL to analyze</param>
    /// <param name="startDate">
    /// Optional start date for filtering analytics (inclusive). 
    /// If null, includes all data from the beginning of time.
    /// </param>
    /// <param name="endDate">
    /// Optional end date for filtering analytics (inclusive).
    /// If null, includes all data up to the current time.
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A <see cref="ClickAnalytics"/> object containing comprehensive analytics data including total clicks,
    /// geographical breakdowns, device analytics, traffic source attribution, and time-based trends.
    /// </returns>
    /// <remarks>
    /// This method compiles comprehensive analytics from multiple data dimensions:
    /// 
    /// Core Metrics:
    /// - Total click count (filtered by date range if specified)
    /// - Click distribution across different categories
    /// 
    /// Geographical Analytics:
    /// - Clicks by country with counts
    /// - Geographic distribution for regional insights
    /// 
    /// Device and Browser Analytics:
    /// - Clicks by device type (Mobile, Desktop, Tablet)
    /// - Browser and operating system breakdowns
    /// 
    /// Traffic Source Attribution:
    /// - Clicks by traffic source (Direct, Search, Social, etc.)
    /// - Campaign performance analysis
    /// 
    /// Temporal Analytics (when date range specified):
    /// - Daily click trends within the specified period
    /// - Time-based pattern analysis
    /// 
    /// Date Range Behavior:
    /// - Both null: Returns all-time analytics
    /// - Start only: Returns data from start date forward
    /// - End only: Returns data up to end date
    /// - Both specified: Returns data within the range (both dates inclusive)
    /// 
    /// The method is optimized for dashboard and reporting scenarios, providing
    /// all necessary data for comprehensive URL performance analysis.
    /// </remarks>
    /// <example>
    /// <code>
    /// // All-time analytics
    /// var allTimeAnalytics = await service.GetAnalyticsAsync(12345);
    /// 
    /// // Last 30 days analytics
    /// var endDate = DateTime.UtcNow;
    /// var startDate = endDate.AddDays(-30);
    /// var monthlyAnalytics = await service.GetAnalyticsAsync(12345, startDate, endDate);
    /// 
    /// // Access different analytics dimensions
    /// Console.WriteLine($"Total clicks: {monthlyAnalytics.TotalClicks}");
    /// Console.WriteLine($"Top country: {monthlyAnalytics.ClicksByCountry.OrderByDescending(x => x.Value).First()}");
    /// </code>
    /// </example>
    Task<ClickAnalytics> GetAnalyticsAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Generates real-time analytics for a shortened URL covering the last 24 hours of activity.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the shortened URL to analyze</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A <see cref="ClickAnalytics"/> object containing analytics data for the past 24 hours,
    /// including recent click patterns, traffic sources, and geographical distribution.
    /// </returns>
    /// <remarks>
    /// This method provides a specialized view of URL performance focused on recent activity:
    /// 
    /// Time Window: 
    /// - Covers exactly 24 hours from the current UTC time backwards
    /// - Updates automatically as time progresses (rolling 24-hour window)
    /// 
    /// Use Cases:
    /// - Dashboard real-time monitoring
    /// - Recent campaign performance tracking  
    /// - Quick health checks on URL activity
    /// - Detecting traffic spikes or anomalies
    /// 
    /// Data Included:
    /// - Total clicks in the past 24 hours
    /// - Geographical distribution of recent traffic
    /// - Device type breakdown for recent clicks
    /// - Traffic source attribution for recent activity
    /// - Daily click trends (single data point for the 24-hour period)
    /// 
    /// Performance Considerations:
    /// - Optimized for frequent dashboard refreshes
    /// - Smaller dataset improves query performance
    /// - Suitable for real-time monitoring scenarios
    /// 
    /// This method is essentially a convenience wrapper around GetAnalyticsAsync
    /// with a predefined 24-hour time window, making it ideal for dashboards
    /// and monitoring applications that need current activity insights.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get real-time analytics for dashboard
    /// var realTimeData = await service.GetRealTimeAnalyticsAsync(12345);
    /// 
    /// // Display recent activity
    /// Console.WriteLine($"Clicks in last 24h: {realTimeData.TotalClicks}");
    /// Console.WriteLine($"Top recent country: {realTimeData.ClicksByCountry.OrderByDescending(x => x.Value).FirstOrDefault()}");
    /// 
    /// // Use for monitoring alerts
    /// if (realTimeData.TotalClicks > alertThreshold)
    /// {
    ///     await SendTrafficAlert(realTimeData);
    /// }
    /// </code>
    /// </example>
    Task<ClickAnalytics> GetRealTimeAnalyticsAsync(long shortUrlId, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Retrieves the most recent click events for a shortened URL in reverse chronological order.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the shortened URL</param>
    /// <param name="count">The maximum number of recent clicks to retrieve (default: 10, must be positive)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A collection of the most recent <see cref="ClickEvent"/> objects, ordered by click timestamp (newest first).
    /// Returns fewer items than requested if insufficient clicks exist.
    /// </returns>
    /// <remarks>
    /// This method provides access to the latest click activity for detailed inspection:
    /// 
    /// Ordering: 
    /// - Results are ordered by ClickedAt timestamp in descending order (newest first)
    /// - Most recent click appears first in the collection
    /// 
    /// Use Cases:
    /// - Activity monitoring and debugging
    /// - Recent user behavior analysis
    /// - Real-time dashboard displays
    /// - Fraud detection and security monitoring
    /// - User experience tracking
    /// 
    /// Data Completeness:
    /// - Each click event includes full enriched data
    /// - Geographical information, device details, traffic source attribution
    /// - UTM parameters for campaign tracking
    /// - Complete user agent and referrer information
    /// 
    /// Performance Considerations:
    /// - Optimized for small result sets (typically 10-50 items)
    /// - Uses read-only database queries for better performance
    /// - Efficient for dashboard and monitoring scenarios
    /// 
    /// Count Parameter:
    /// - Default of 10 is suitable for most dashboard scenarios
    /// - Can be adjusted based on UI requirements
    /// - Large values may impact performance
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get recent clicks for monitoring
    /// var recentClicks = await service.GetRecentClicksAsync(12345, 5);
    /// 
    /// // Display recent activity
    /// foreach (var click in recentClicks)
    /// {
    ///     Console.WriteLine($"Click at {click.ClickedAt} from {click.Country} using {click.Browser}");
    /// }
    /// 
    /// // Check for suspicious activity
    /// var suspiciousClicks = recentClicks.Where(c => c.IpAddress.StartsWith("suspicious-range"));
    /// </code>
    /// </example>
    Task<IEnumerable<ClickEvent>> GetRecentClicksAsync(long shortUrlId, int count = 10, CancellationToken cancellationToken = default);
 
    
    /// <summary>
    /// Retrieves paginated click history for a shortened URL with comprehensive pagination metadata.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the shortened URL</param>
    /// <param name="pageNumber">The page number to retrieve (1-based indexing, must be positive)</param>
    /// <param name="pageSize">The number of clicks per page (default: 50, must be positive)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A <see cref="PaginatedResult{ClickEvent}"/> containing the requested page of click events
    /// along with pagination metadata (total count, page info, etc.)
    /// </returns>
    /// <remarks>
    /// This method provides efficient access to large click datasets with proper pagination:
    /// 
    /// Pagination Details:
    /// - Uses 1-based page indexing (first page = 1)
    /// - Default page size of 50 balances performance and usability
    /// - Returns total count for pagination UI calculations
    /// 
    /// Data Ordering:
    /// - Clicks are ordered by timestamp in descending order (newest first)
    /// - Consistent ordering across pagination requests
    /// 
    /// Performance Optimization:
    /// - Uses efficient database skip/take operations
    /// - Separate optimized query for total count
    /// - Read-only queries for better performance
    /// 
    /// Return Data Structure:
    /// - **Items**: The actual click events for the requested page
    /// - **TotalCount**: Total number of clicks (for pagination calculations)
    /// - **PageNumber**: The requested page number
    /// - **PageSize**: The requested page size
    /// - Additional properties: HasNextPage, HasPreviousPage, TotalPages
    /// 
    /// Use Cases:
    /// - Admin interfaces for detailed click analysis
    /// - Data export and reporting functionality
    /// - Historical trend analysis
    /// - Compliance and audit requirements
    /// - Bulk data processing scenarios
    /// 
    /// Large Dataset Handling:
    /// - Efficient for tables with millions of click events
    /// - Avoids memory issues with large result sets
    /// - Suitable for web UI pagination controls
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get first page of click history
    /// var firstPage = await service.GetClickHistoryAsync(12345, 1, 25);
    /// 
    /// // Display pagination info
    /// Console.WriteLine($"Showing {firstPage.Items.Count()} of {firstPage.TotalCount} clicks");
    /// Console.WriteLine($"Page {firstPage.PageNumber} of {firstPage.TotalPages}");
    /// 
    /// // Process clicks on current page
    /// foreach (var click in firstPage.Items)
    /// {
    ///     // Process each click event
    ///     ProcessClickEvent(click);
    /// }
    /// 
    /// // Get next page if available
    /// if (firstPage.HasNextPage)
    /// {
    ///     var nextPage = await service.GetClickHistoryAsync(12345, firstPage.PageNumber + 1, 25);
    /// }
    /// </code>
    /// </example>
    Task<PaginatedResult<ClickEvent>> GetClickHistoryAsync(long shortUrlId, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Performs cleanup of old click events based on a specified retention period to manage database size and comply with data retention policies.
    /// </summary>
    /// <param name="retentionPeriod">
    /// The time span defining how long to retain click data. 
    /// Click events older than this period from the current UTC time will be deleted.
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// The number of click events that were successfully deleted from the database
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database deletion operation fails</exception>
    /// <remarks>
    /// This method provides automated data lifecycle management for click tracking data:
    /// 
    /// Retention Logic:
    /// - Calculates cutoff date as: Current UTC Time - Retention Period
    /// - Deletes all click events with ClickedAt timestamp before the cutoff date
    /// - Uses efficient bulk deletion operations for performance
    /// 
    /// Common Retention Periods:
    /// - `TimeSpan.FromDays(90)` - 3 months retention
    /// - `TimeSpan.FromDays(365)` - 1 year retention  
    /// - `TimeSpan.FromDays(730)` - 2 years retention
    /// - Custom periods based on business or legal requirements
    /// 
    /// Use Cases:
    /// - Compliance**: Meeting GDPR, CCPA, and other data retention regulations
    /// - Performance: Maintaining database performance by limiting data growth
    /// - Storage Cost: Reducing storage costs for cloud-hosted databases
    /// - Maintenance: Regular housekeeping as part of system maintenance
    /// 
    /// Operational Considerations:
    /// - Irreversible: Deleted data cannot be recovered without backups
    /// - Performance: May impact database performance during execution on large datasets
    /// - Analytics Impact: Historical analytics will be affected by data removal
    /// - Backup Recommendation: Consider backing up data before deletion
    /// 
    /// Scheduling Recommendations:
    /// - Run during off-peak hours to minimize performance impact
    /// - Consider running incrementally for very large datasets
    /// - Monitor execution time and database performance
    /// - Implement alerts for unexpected deletion counts
    /// 
    /// Logging and Monitoring:
    /// - Automatically logs the number of deleted records
    /// - Includes timestamp information for audit trails
    /// - Suitable for monitoring and alerting systems
    /// </remarks>
    /// <example>
    /// <code>
    /// // Delete clicks older than 90 days
    /// var retentionPeriod = TimeSpan.FromDays(90);
    /// var deletedCount = await service.CleanupOldClicksAsync(retentionPeriod);
    /// Console.WriteLine($"Cleaned up {deletedCount} old click events");
    /// 
    /// // More conservative 2-year retention
    /// var longRetention = TimeSpan.FromDays(730);
    /// await service.CleanupOldClicksAsync(longRetention);
    /// 
    /// // Scheduled cleanup (typically in background service)
    /// public async Task PerformDailyCleanup()
    /// {
    ///     try
    ///     {
    ///         var deleted = await clickTrackingService.CleanupOldClicksAsync(TimeSpan.FromDays(365));
    ///         logger.LogInformation("Daily cleanup completed: {DeletedCount} records", deleted);
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         logger.LogError(ex, "Failed to perform daily cleanup");
    ///         // Handle cleanup failure (retry, alert, etc.)
    ///     }
    /// }
    /// </code>
    /// </example>
    Task<int> CleanupOldClicksAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}
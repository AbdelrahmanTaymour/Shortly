using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.ClickTracking;

/// <summary>
/// Defines comprehensive operations for tracking, retrieving, and analyzing URL click data.
/// </summary>
public interface IClickEventRepository
{
    /// <summary>
    /// Creates a new click event record in the database.
    /// </summary>
    /// <param name="clickEvent">The click event entity to be created</param>
    /// <returns>The created click event with any database-generated values populated</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails</exception>
    Task<ClickEvent> CreateAsync(ClickEvent clickEvent);
   
    
    /// <summary>
    /// Retrieves a click event by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the click event to retrieve</param>
    /// <param name="includeTracking">
    /// If true, enables change tracking for the returned entity; 
    /// if false, returns a read-only entity with better performance
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// The click event entity if found; otherwise null
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails</exception>
    Task<ClickEvent?> GetByIdAsync(Guid id, bool includeTracking = false, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Retrieves paginated click events for a specific shortened URL.
    /// </summary>
    /// <param name="shortUrlId">The identifier of the shortened URL</param>
    /// <param name="pageNumber">The page number to retrieve (1-based indexing)</param>
    /// <param name="pageSize">The number of records per page (default: 50)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A collection of click events ordered by click timestamp (most recent first)
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when pageNumber is less than 1 or pageSize is less than 1</exception>
    /// <remarks>
    /// Results are ordered by ClickedAt in descending order (newest first).
    /// Use pagination to handle large datasets efficiently.
    /// </remarks>
    Task<IEnumerable<ClickEvent>> GetByShortUrlIdAsync(long shortUrlId, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Retrieves paginated click events within a specified date range, optionally filtered by shortened URL.
    /// </summary>
    /// <param name="startDate">The start date of the range (inclusive)</param>
    /// <param name="endDate">The end date of the range (inclusive)</param>
    /// <param name="shortUrlId">Optional shortened URL identifier to filter results</param>
    /// <param name="pageNumber">The page number to retrieve (1-based indexing)</param>
    /// <param name="pageSize">The number of records per page (default: 50)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A collection of click events within the specified date range, ordered by click timestamp (the most recent first)
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when pageNumber is less than 1, or pageSize is less than 1</exception>
    /// <exception cref="ArgumentException">Thrown when startDate is greater than endDate</exception>
    /// <remarks>
    /// If shortUrlId is null, returns clicks for all URLs within the date range.
    /// Results are ordered by ClickedAt in descending order (newest first).
    /// Both startDate and endDate are inclusive in the query.
    /// </remarks>
    Task<IEnumerable<ClickEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, long? shortUrlId = null, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Gets the total count of clicks for a specific shortened URL.
    /// </summary>
    /// <param name="shortUrlId">The identifier of the shortened URL</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>The total number of clicks for the specified URL</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails</exception>
    /// <remarks>
    /// This method counts all click events associated with the specified shortened URL,
    /// regardless of when they occurred.
    /// </remarks>
    Task<int> GetTotalClicksAsync(long shortUrlId, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Gets the total count of clicks for a specific shortened URL within a date range.
    /// </summary>
    /// <param name="shortUrlId">The identifier of the shortened URL</param>
    /// <param name="startDate">The start date of the range (inclusive)</param>
    /// <param name="endDate">The end date of the range (inclusive)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>The total number of clicks for the specified URL within the date range</returns>
    /// <exception cref="DatabaseException">Thrown when the database operation fails</exception>
    /// <exception cref="ArgumentException">Thrown when startDate is greater than endDate</exception>
    /// <remarks>
    /// Both startDate and endDate are inclusive in the count calculation.
    /// This method is useful for generating time-based analytics and reports.
    /// </remarks>
    Task<int> GetTotalClicksInDateRangeAsync(long shortUrlId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Gets click statistics grouped by country for a specific shortened URL.
    /// </summary>
    /// <param name="shortUrlId">The identifier of the shortened URL</param>
    /// <param name="startDate">Optional start date to filter results (inclusive)</param>
    /// <param name="endDate">Optional end date to filter results (inclusive)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A dictionary where keys are country names and values are click counts.
    /// Countries with null values are grouped under their actual null key.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when the database operation fails</exception>
    /// <exception cref="ArgumentException">Thrown when startDate is greater than endDate</exception>
    /// <remarks>
    /// If both startDate and endDate are null, returns statistics for all time.
    /// If only startDate is provided, returns statistics from that date forward.
    /// If only the endDate is provided, it returns statistics up to that date.
    /// Countries are identified based on the Country property of click events.
    /// </remarks>
    Task<Dictionary<string, int>> GetClicksByCountryAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Gets click statistics grouped by device type for a specific shortened URL.
    /// </summary>
    /// <param name="shortUrlId">The identifier of the shortened URL</param>
    /// <param name="startDate">Optional start date to filter results (inclusive)</param>
    /// <param name="endDate">Optional end date to filter results (inclusive)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A dictionary where keys are device type names and values are click counts.
    /// Null or empty device types are grouped under "Unknown".
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when the database operation fails</exception>
    /// <exception cref="ArgumentException">Thrown when startDate is greater than endDate</exception>
    /// <remarks>
    /// Device types might include values like "Desktop", "Mobile", "Tablet", etc.
    /// If both startDate and endDate are null, returns statistics for all time.
    /// Click events with null DeviceType values are automatically grouped under "Unknown".
    /// </remarks>
    Task<Dictionary<string, int>> GetClicksByDeviceTypeAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Gets click statistics grouped by traffic source for a specific shortened URL.
    /// </summary>
    /// <param name="shortUrlId">The identifier of the shortened URL</param>
    /// <param name="startDate">Optional start date to filter results (inclusive)</param>
    /// <param name="endDate">Optional end date to filter results (inclusive)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A dictionary where keys are traffic source names and values are click counts.
    /// Null or empty traffic sources are grouped under "Unknown".
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when the database operation fails</exception>
    /// <exception cref="ArgumentException">Thrown when startDate is greater than endDate</exception>
    /// <remarks>
    /// Traffic sources might include values like "Direct", "Google", "Facebook", "Email", etc.
    /// If both startDate and endDate are null, returns statistics for all time.
    /// Click events with null TrafficSource values are automatically grouped under "Unknown".
    /// This data is valuable for understanding how users discover your shortened URLs.
    /// </remarks>
    Task<Dictionary<string, int>> GetClicksByTrafficSourceAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Gets daily click statistics for a specific shortened URL within a date range.
    /// </summary>
    /// <param name="shortUrlId">The identifier of the shortened URL</param>
    /// <param name="startDate">The start date of the range (inclusive)</param>
    /// <param name="endDate">The end date of the range (inclusive)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A dictionary where keys are dates (DateTime with time set to 00:00:00) and values are click counts for that day
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when the database operation fails</exception>
    /// <exception cref="ArgumentException">Thrown when startDate is greater than endDate</exception>
    /// <remarks>
    /// Each key in the returned dictionary represents a calendar day (date only, no time component).
    /// Days with zero clicks within the range will not appear in the dictionary.
    /// This method is ideal for generating daily click trend charts and reports.
    /// All times are normalized to the date component only (00:00:00 time).
    /// </remarks>
    Task<Dictionary<DateTime, int>> GetDailyClicksAsync(long shortUrlId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Gets hourly click statistics for a specific shortened URL on a specific date.
    /// </summary>
    /// <param name="shortUrlId">The identifier of the shortened URL</param>
    /// <param name="date">The specific date to analyze (a time component is ignored)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A dictionary where keys are hour numbers (0-23) and values are click counts for that hour
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails</exception>
    /// <remarks>
    /// The date parameter's time component is ignored; only the date part is used.
    /// Hours with zero clicks will not appear in the dictionary.
    /// Hour keys range from 0 (midnight) to 23 (11 PM).
    /// This method is useful for understanding click patterns throughout a day.
    /// </remarks>
    Task<Dictionary<int, int>> GetHourlyClicksAsync(long shortUrlId, DateTime date, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Gets the most recent click events for a specific shortened URL.
    /// </summary>
    /// <param name="shortUrlId">The identifier of the shortened URL</param>
    /// <param name="count">The maximum number of recent clicks to retrieve (default: 10)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>
    /// A collection of the most recent click events, ordered by click timestamp (the most recent first)
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when the database operation fails</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the count is less than 1</exception>
    /// <remarks>
    /// Results are ordered by ClickedAt in descending order (newest first).
    /// If there are fewer clicks than the requested count, all available clicks are returned.
    /// This method is useful for displaying recent activity on dashboards or for debugging purposes.
    /// </remarks>
    Task<IEnumerable<ClickEvent>> GetRecentClicksAsync(long shortUrlId, int count = 10, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Checks whether a click event with the specified identifier exists in the database.
    /// </summary>
    /// <param name="id">The unique identifier of the click event to check</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>True if a click event with the specified ID exists; otherwise false</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails</exception>
    /// <remarks>
    /// This method performs an efficient existence check without retrieving the full entity.
    /// Use this method when you only need to verify existence without accessing the entity data.
    /// </remarks>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Deletes click events that are older than the specified date from the database.
    /// This is typically used for data cleanup and archival purposes.
    /// </summary>
    /// <param name="olderThan">The cutoff date; click events with ClickedAt before this date will be deleted</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation</param>
    /// <returns>The number of click events that were deleted</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails</exception>
    /// <remarks>
    /// This method permanently deletes data and cannot be undone.
    /// Use with caution and ensure you have proper backups if needed.
    /// Consider the impact on analytics and reporting when deleting historical data.
    /// The operation uses ExecuteDeleteAsync for efficient bulk deletion.
    /// Click events with ClickedAt exactly equal to olderThan are NOT deleted (strict less-than comparison).
    /// </remarks>
    Task<int> DeleteOldClicksAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
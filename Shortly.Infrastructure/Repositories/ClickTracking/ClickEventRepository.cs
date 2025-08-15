using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.ClickTracking;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.ClickTracking;

/// <summary>
/// Repository implementation for managing click events in the database.
/// Provides comprehensive operations for tracking, retrieving, and analyzing URL click data.
/// </summary>
/// <remarks>
/// This repository handles all database operations related to click events including
/// - Creating new click events
/// - Retrieving click events by various criteria
/// - Generating analytics data (clicks by country, device, traffic source, etc.)
/// - Managing click event lifecycle (creation and deletion)
/// </remarks>
/// <param name="dbContext">The SQL Server database context for data operations</param>
/// <param name="logger">Logger instance for recording operations and errors</param>
public class ClickEventRepository(SQLServerDbContext dbContext, ILogger<ClickEventRepository> logger) : IClickEventRepository
{
    /// <inheritdoc/>
    public async Task<ClickEvent> CreateAsync(ClickEvent clickEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.ClickEvents.AddAsync(clickEvent, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return clickEvent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error tracking short URL with short url Id: {ShortId}", clickEvent.ShortUrlId);
            throw new DatabaseException("Failed to tracking short UR", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<ClickEvent?> GetByIdAsync(Guid id, bool includeTracking = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = includeTracking ? dbContext.ClickEvents : dbContext.ClickEvents.AsNoTracking();
            return await query
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving click event by ID: {ClickId}", id);
            throw new DatabaseException("Failed to retrieve click event by ID", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<IEnumerable<ClickEvent>> GetByShortUrlIdAsync(long shortUrlId, int pageNumber = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ClickEvents
                .AsNoTracking()
                .Where(c => c.ShortUrlId == shortUrlId)
                .OrderByDescending(c => c.ClickedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding ShortUrl clicks");
            throw new DatabaseException("Failed to find ShortUrl clicks", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<IEnumerable<ClickEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, long? shortUrlId = null, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.ClickEvents
                .Where(c => c.ClickedAt >= startDate && c.ClickedAt <= endDate);
            
            if(shortUrlId.HasValue)
                query = query.Where(c => c.ShortUrlId == shortUrlId);
            
            return await query
                .AsNoTracking()
                .OrderByDescending(c => c.ClickedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding ShortUrl clicks in date range {startDate} - {endDate}", startDate, endDate);
            throw new DatabaseException("Failed to find ShortUrl clicks in date range", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<int> GetTotalClicksAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ClickEvents
                .AsNoTracking()
                .Where(c => c.ShortUrlId == shortUrlId)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving total clicks for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve total clicks", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<int> GetTotalClicksInDateRangeAsync(long shortUrlId, DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ClickEvents
                .AsNoTracking()
                .Where(c => 
                            c.ShortUrlId == shortUrlId && 
                            c.ClickedAt >= startDate && 
                            c.ClickedAt <= endDate)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving total clicks in date range: {startDate} - {endDate}", startDate, endDate);
            throw new DatabaseException("Failed to retrieve total clicks in date range", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetClicksByCountryAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.ClickEvents
                .Where(c => c.ShortUrlId == shortUrlId);
            
            if(startDate.HasValue)
                query = query.Where(c => c.ClickedAt >= startDate.Value);
            
            if (endDate.HasValue)
                query = query.Where(c => c.ClickedAt <= endDate.Value);
            
            return await query
                .AsNoTracking()
                .GroupBy(c => c.Country)
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Country, x => x.Count, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving clicks count per country for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve clicks count per country", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetClicksByDeviceTypeAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.ClickEvents
                .Where(c => c.ShortUrlId == shortUrlId);
            
            if(startDate.HasValue)
                query = query.Where(c => c.ClickedAt >= startDate.Value);
            
            if (endDate.HasValue)
                query = query.Where(c => c.ClickedAt <= endDate.Value);
            
            return await query
                .AsNoTracking()
                .GroupBy(c => c.DeviceType ?? "Unknown")
                .Select(g => new { DeviceType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DeviceType, x => x.Count, cancellationToken);
            
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving clicks count per device for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve clicks count per device", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetClicksByTrafficSourceAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.ClickEvents
                .Where(c => c.ShortUrlId == shortUrlId);
            
            if(startDate.HasValue)
                query = query.Where(c => c.ClickedAt >= startDate.Value);
            
            if (endDate.HasValue)
                query = query.Where(c => c.ClickedAt <= endDate.Value);
            
            return await query
                .AsNoTracking()
                .GroupBy(c => c.TrafficSource ?? "Unknown")
                .Select(g => new { TrafficSource = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TrafficSource, x => x.Count, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving clicks count per traffic source for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve clicks count per traffic source", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<Dictionary<DateTime, int>> GetDailyClicksAsync(long shortUrlId, DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ClickEvents
                .AsNoTracking()
                .Where(c => c.ShortUrlId == shortUrlId && c.ClickedAt >= startDate && c.ClickedAt <= endDate)
                .GroupBy(c => c.ClickedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving daily clicks for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve daily clicks for ShortUrl", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<Dictionary<int, int>> GetHourlyClicksAsync(long shortUrlId, DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);
        
        try
        {
            return await dbContext.ClickEvents
                .AsNoTracking()
                .Where(c => c.ShortUrlId == shortUrlId && c.ClickedAt >= startOfDay && c.ClickedAt < endOfDay)
                .GroupBy(c => c.ClickedAt.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Hour, x => x.Count, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving hourly clicks for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve hourly clicks for ShortUrl", ex);
        }
    }

    
    /// <inheritdoc/>
    public async Task<IEnumerable<ClickEvent>> GetRecentClicksAsync(long shortUrlId, int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ClickEvents
                .AsNoTracking()
                .Where(c => c.ShortUrlId == shortUrlId)
                .OrderByDescending(c => c.ClickedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving recent clicks for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve recent clicks for ShortUrl", ex);
        }
    }

   
    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ClickEvents
                .AsNoTracking()
                .AnyAsync(c => c.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Error checking if the click event with ID '{Id}' exists", id);
            throw new DatabaseException("Failed to check if URL exists", ex);
        }
    }

   
    /// <inheritdoc/>
    public async Task<int> DeleteOldClicksAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ClickEvents
                .AsNoTracking()
                .Where(c => c.ClickedAt < olderThan)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Error deleting click events that order than '{orderThan}'", olderThan);
            throw new DatabaseException($"Failed to delete click events that older than '{olderThan}'", ex);
        }
    }
}
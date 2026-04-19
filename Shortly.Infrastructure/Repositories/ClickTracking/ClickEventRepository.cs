using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;
using Shortly.Domain.RepositoryContract.ClickTracking;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.ClickTracking;

/// <summary>
/// Repository implementation for managing click events in the database.
/// All methods that can be called in parallel use <see cref="IDbContextFactory{TContext}"/>
/// to create isolated, independent DbContext instances per operation, avoiding concurrent
/// access violations when called via Task.WhenAll.
/// </summary>
public class ClickEventRepository(
    IDbContextFactory<SqlServerDbContext> dbContextFactory,
    SqlServerDbContext dbContext,
    ILogger<ClickEventRepository> logger)
    : IClickEventRepository
{
    #region Write & Simple Read (use shared dbContext — called sequentially)

    /// <inheritdoc/>
    public async Task<ClickEvent> CreateAsync(ClickEvent clickEvent)
    {
        try
        {
            await dbContext.ClickEvents.AddAsync(clickEvent).ConfigureAwait(false);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            return clickEvent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error tracking short URL with Id: {ShortId}", clickEvent.ShortUrlId);
            throw new DatabaseException("Failed to track short URL", ex);
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
    public async Task<IEnumerable<ClickEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, long? shortUrlId = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.ClickEvents
                .Where(c => c.ClickedAt >= startDate && c.ClickedAt <= endDate);

            if (shortUrlId.HasValue)
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
            logger.LogError(ex, "Error finding ShortUrl clicks in date range {StartDate} - {EndDate}", startDate, endDate);
            throw new DatabaseException("Failed to find ShortUrl clicks in date range", ex);
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
            logger.LogError("Error checking if click event '{Id}' exists", id);
            throw new DatabaseException("Failed to check if click event exists", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOldClicksAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ClickEvents
                .Where(c => c.ClickedAt < olderThan)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Error deleting click events older than '{OlderThan}'", olderThan);
            throw new DatabaseException($"Failed to delete click events older than '{olderThan}'", ex);
        }
    }

    #endregion

    #region Parallel-Safe Single-URL Reads (use dbContextFactory)

    /// <inheritdoc/>
    public async Task<int> GetTotalClicksAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await ctx.ClickEvents
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
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await ctx.ClickEvents
                .AsNoTracking()
                .Where(c => c.ShortUrlId == shortUrlId && c.ClickedAt >= startDate && c.ClickedAt <= endDate)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving total clicks in date range: {StartDate} - {EndDate}", startDate, endDate);
            throw new DatabaseException("Failed to retrieve total clicks in date range", ex);
        }
    }


    /// <inheritdoc/>
    public async Task<DateTime?> GetFirstClickAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await ctx.ClickEvents
                .AsNoTracking()
                .Where(c => c.ShortUrlId == shortUrlId)
                .MinAsync(c => (DateTime?)c.ClickedAt, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving first click for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve first click timestamp", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<DateTime?> GetLastClickAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await ctx.ClickEvents
                .AsNoTracking()
                .Where(c => c.ShortUrlId == shortUrlId)
                .MaxAsync(c => (DateTime?)c.ClickedAt, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving last click for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve last click timestamp", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetClicksByCountryAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = ctx.ClickEvents.Where(c => c.ShortUrlId == shortUrlId);

            if (startDate.HasValue) query = query.Where(c => c.ClickedAt >= startDate.Value);
            if (endDate.HasValue) query = query.Where(c => c.ClickedAt <= endDate.Value);

            return await query
                .AsNoTracking()
                .GroupBy(c => c.Country)
                .Select(g => new { Country = g.Key ?? "Unknown", Count = g.Count() })
                .ToDictionaryAsync(x => x.Country, x => x.Count, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving clicks by country for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve clicks by country", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetClicksByDeviceTypeAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = ctx.ClickEvents.Where(c => c.ShortUrlId == shortUrlId);

            if (startDate.HasValue) query = query.Where(c => c.ClickedAt >= startDate.Value);
            if (endDate.HasValue) query = query.Where(c => c.ClickedAt <= endDate.Value);

            return await query
                .AsNoTracking()
                .GroupBy(c => c.DeviceType ?? "Unknown")
                .Select(g => new { DeviceType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DeviceType, x => x.Count, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving clicks by device for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve clicks by device", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetClicksByTrafficSourceAsync(long shortUrlId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = ctx.ClickEvents.Where(c => c.ShortUrlId == shortUrlId);

            if (startDate.HasValue) query = query.Where(c => c.ClickedAt >= startDate.Value);
            if (endDate.HasValue) query = query.Where(c => c.ClickedAt <= endDate.Value);

            return await query
                .AsNoTracking()
                .GroupBy(c => c.TrafficSource ?? "Direct")
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Source, x => x.Count, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving clicks by traffic source for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve clicks by traffic source", ex);
        }
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<DateTime, int>> GetDailyClicksAsync(long shortUrlId, DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await ctx.ClickEvents
                .AsNoTracking()
                .Where(c => c.ShortUrlId == shortUrlId && c.ClickedAt >= startDate && c.ClickedAt <= endDate)
                .GroupBy(c => c.ClickedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving daily clicks for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve daily clicks", ex);
        }
    }
    #endregion
}
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Domain.RepositoryContract.ShortUrls;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.ShortUrls;

public class ShortUrlQueryRepository(
    IDbContextFactory<SqlServerDbContext> dbContextFactory,
    ILogger<ShortUrlQueryRepository> logger)
    : IShortUrlQueryRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrl>> SearchAsync(Expression<Func<ShortUrl, bool>> predicate, int pageNumber,
        int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            throw new ValidationException("Page number must be greater than 0");

        if (pageSize is < 1 or > 1000)
            throw new ValidationException("Page size must be between 1 and 1000");

        try
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await ctx.ShortUrls
                .AsNoTracking()
                .Where(predicate)
                .OrderByDescending(s => s.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching short URLs: Page {Page}, PageSize {PageSize}", pageNumber, pageSize);
            throw new DatabaseException("Failed to search short URLs", ex);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<ShortUrl>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(s => s.UserId == userId, pageNumber, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ShortUrl>> GetByOrganizationIdAsync(Guid organizationId, int pageNumber,
        int pageSize, CancellationToken cancellationToken = default)
    {
        return SearchAsync(s => s.OrganizationId == organizationId, pageNumber, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ShortUrl>> GetAnonymousUrlsByDateRangeAsync(DateTime startDate, DateTime endDate,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be greater than end date");

        return SearchAsync(
            s => s.OwnerType == enShortUrlOwnerType.Anonymous && s.CreatedAt >= startDate && s.CreatedAt <= endDate,
            pageNumber, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ShortUrl>> GetExpiredAsync(DateTime nowUtc, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(s => s.ExpiresAt != null && s.ExpiresAt <= nowUtc, pageNumber, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ShortUrl>> GetPrivateLinksAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(s => s.UserId == userId && s.IsPrivate, pageNumber, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ShortUrl>> GetByDateRangeAsync(DateTime startDate, DateTime endDate,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be greater than end date");

        return SearchAsync(
            s => s.CreatedAt >= startDate && s.CreatedAt <= endDate,
            pageNumber, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IGrouping<string, ShortUrl>>> GetDuplicateUrlsAsync(Guid? userId = null,
        Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Single isolated context for both queries — they are sequential, not parallel,
            // so one context is correct and safe here.
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var query = ctx.ShortUrls.AsNoTracking();

            if (userId.HasValue)
                query = query.Where(s => s.UserId == userId);

            if (organizationId.HasValue)
                query = query.Where(s => s.OrganizationId == organizationId);

            var duplicateKeys = await query
                .GroupBy(s => s.OriginalUrl)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            if (duplicateKeys.Count == 0)
                return [];

            var duplicates = await query
                .Where(s => duplicateKeys.Contains(s.OriginalUrl))
                .ToListAsync(cancellationToken);

            return duplicates.GroupBy(s => s.OriginalUrl).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding duplicate URLs");
            throw new DatabaseException("Failed to find duplicate URLs", ex);
        }
    }

    /// <summary>
    ///     Analytics-optimised read: returns only the columns needed by
    ///     <c>UrlStatisticsService.FetchAllUserUrlsAsync</c>, avoiding the cost of
    ///     fetching PasswordHash, Description, IpAddress, and other unused fields.
    /// </summary>
    public async Task<IEnumerable<ShortUrl>> GetUserUrlsForAnalyticsAsync(
        Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1");

        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Project to only the 7 columns analytics needs.
        // EF Core generates a tight SELECT with no JOINs — SQL Server can satisfy
        return await ctx.ShortUrls
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ShortUrl
            {
                Id = s.Id,
                UserId = s.UserId,
                ShortCode = s.ShortCode,
                OriginalUrl = s.OriginalUrl,
                CreatedAt = s.CreatedAt,
                TotalClicks = s.TotalClicks,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ShortUrl>> GetUnusedUrlsAsync(TimeSpan? olderThan = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        Expression<Func<ShortUrl, bool>> predicate = olderThan.HasValue
            ? s => s.TotalClicks == 0 && s.CreatedAt <= DateTime.UtcNow - olderThan.Value
            : s => s.TotalClicks == 0;

        return SearchAsync(predicate, pageNumber, pageSize, cancellationToken);
    }

    
    public async Task<(IEnumerable<ShortUrl> items, int totalCount)> SearchByFiltersAsync(
        Guid userId,
        string? search,
        string? status,
        string? visibility,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortBy = "newest",
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Base scope: always restrict to the owner
            var query = ctx.ShortUrls
                .AsNoTracking()
                .Where(s => s.UserId == userId);

            // Free-text filter
            // Applied against Title and OriginalUrl (both nullable-safe).
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s =>
                    (s.Title != null && s.Title.ToLower().Contains(term)) ||
                    s.OriginalUrl.ToLower().Contains(term));
            }

            // Status filter
            // "active" → IsActive == true | "inactive" → IsActive == false
            if (!string.IsNullOrWhiteSpace(status))
            {
                var isActive = status.Trim().Equals("active", StringComparison.CurrentCultureIgnoreCase);
                query = query.Where(s => s.IsActive == isActive);
            }

            // Visibility filter
            // "private" → IsPrivate == true  |  "public" → IsPrivate == false
            if (!string.IsNullOrWhiteSpace(visibility))
            {
                var isPrivate = visibility.Trim().Equals("private", StringComparison.CurrentCultureIgnoreCase);
                query = query.Where(s => s.IsPrivate == isPrivate);
            }

            // Date-range filter (both bounds are inclusive) ──────────────────
            if (dateFrom.HasValue)
                query = query.Where(s => s.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(s => s.CreatedAt <= dateTo.Value);

            // Total count (before pagination) ───────────────────────────────
            // EF Core translates this to a single COUNT(*) — no data rows fetched yet.
            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            // Sorting ────────────────────────────────────────────────────────
            // All four sort keys map to a single EF Core ORDER BY clause.
            query = sortBy.Trim().ToLower() switch
            {
                "oldest" => query.OrderBy(s => s.CreatedAt),
                "most-clicks" => query.OrderByDescending(s => s.TotalClicks),
                "least-clicks" => query.OrderBy(s => s.TotalClicks),
                _ => query.OrderByDescending(s => s.CreatedAt) // "newest" / default
            };

            // ── 8. Pagination ─────────────────────────────────────────────────────
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error in SearchByFiltersAsync: User={UserId} Search={Search} Page={Page}",
                userId, search, pageNumber);
            throw new DatabaseException("Failed to search short URLs", ex);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Analytics.Contracts;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.ShortUrls.DTOs;
using Shortly.Domain.Entities;
using Shortly.Domain.RepositoryContract.Analytics;
using Shortly.Domain.RepositoryContract.ShortUrls;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.Analytics;

/// <summary>
/// Data-access layer for ShortUrl analytics operations.
///
/// Performance notes:
///   <see cref="GetUserAnalyticsAsync"/> and <see cref="GetOrganizationAnalyticsAsync"/> now
///     use <c>Database.SqlQuery&lt;T&gt;</c> (EF Core 8) to bypass the EF translation of
///     <c>GroupBy(s =&gt; 1)</c>, which produced a correlated-subquery plan on SQL Server.
///     The raw SQL executes as a single-pass aggregation scan and reads ~10× less I/O
///     on large tables.
/// </summary>
public class ShortUrlAnalyticsRepository(
    SqlServerDbContext dbContext,
    ILogger<ShortUrlAnalyticsRepository> logger,
    IShortUrlQueryRepository urlQueryRepository)
    : IShortUrlAnalyticsRepository, IShortUrlAnalyticsDapperQueries
{
    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.ShortUrls.AsNoTracking();
            if (activeOnly)
                query = query.Where(s => s.IsActive);
 
            return await query.CountAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving short URLs count");
            throw new DatabaseException("Failed to retrieve URLs count", ex);
        }
    }
 
    /// <inheritdoc />
    public async Task<int> GetTotalClicksAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ShortUrls
                .AsNoTracking()
                .Where(s => s.Id == shortUrlId)
                .Select(s => s.TotalClicks)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving total clicks for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to retrieve total clicks", ex);
        }
    }
 
    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrl>> GetMostPopularUrlAsync(int topCount = 10, TimeSpan? timeframe = null,
        Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (topCount is < 1 or > 100)
            throw new ArgumentException("Top count must be between 1 and 100", nameof(topCount));
 
        try
        {
            var query = dbContext.ShortUrls.AsNoTracking().Where(s => s.IsActive);
 
            if (timeframe.HasValue)
            {
                var cutoff = DateTime.UtcNow.Subtract(timeframe.Value);
                query = query.Where(s => s.CreatedAt >= cutoff);
            }
 
            if (userId.HasValue)
                query = query.Where(s => s.UserId == userId);
 
            return await query
                .OrderByDescending(s => s.TotalClicks)
                .ThenByDescending(s => s.CreatedAt)
                .Take(topCount)
                // Project only the fields the caller needs — avoids fetching large
                // nullable text columns (Description, Password hash, etc.)
                .Select(s => new ShortUrl
                {
                    Id           = s.Id,
                    ShortCode    = s.ShortCode,
                    OriginalUrl  = s.OriginalUrl,
                    Title        = s.Title,
                    TotalClicks  = s.TotalClicks,
                    CreatedAt    = s.CreatedAt,
                    IsActive     = s.IsActive,
                    UserId       = s.UserId,
                    ExpiresAt    = s.ExpiresAt,
                    ClickLimit   = s.ClickLimit,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving most popular URLs");
            throw new DatabaseException("Failed to retrieve popular URLs", ex);
        }
    }
 
    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrl>> GetApproachingLimitAsync(double warningThreshold = 0.8,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (warningThreshold is < 0.0 or > 1.0)
            throw new ArgumentException("Warning threshold must be between 0.0 and 1.0", nameof(warningThreshold));
 
        return await urlQueryRepository.SearchAsync(
            s => s.ClickLimit > 0 && s.TotalClicks >= s.ClickLimit * warningThreshold,
            pageNumber, pageSize, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<UserAnalyticsSummary> GetUserAnalyticsAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Single-pass aggregation scan — no subquery wrapping.
            // Column aliases must exactly match UserAnalyticsSummary property names (EF8 SqlQuery).
            var result = await dbContext.Database
                .SqlQuery<UserAnalyticsSummary>($"""
                    SELECT
                        COUNT(*)                                                               AS TotalUrls,
                        SUM(CASE WHEN IsActive            = 1 THEN 1 ELSE 0 END)              AS ActiveUrls,
                        COALESCE(SUM(CAST(TotalClicks AS BIGINT)), 0)                         AS TotalClicks,
                        SUM(CASE WHEN IsPrivate           = 1 THEN 1 ELSE 0 END)              AS PrivateUrls,
                        SUM(CASE WHEN IsPasswordProtected = 1 THEN 1 ELSE 0 END)              AS PasswordProtectedUrls,
                        SUM(CASE WHEN ExpiresAt IS NOT NULL
                                  AND ExpiresAt <= SYSUTCDATETIME() THEN 1 ELSE 0 END)        AS ExpiredUrls
                    FROM ShortUrls WITH (NOLOCK)
                    WHERE UserId = {userId}
                    """)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
 
            return result ?? new UserAnalyticsSummary();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving analytics for user: {UserId}", userId);
            throw new DatabaseException("Failed to retrieve user analytics", ex);
        }
    }
 
    /// <inheritdoc />
    public async Task<OrganizationAnalyticsSummary> GetOrganizationAnalyticsAsync(Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await dbContext.Database
                .SqlQuery<OrganizationAnalyticsSummary>($"""
                    SELECT
                        COUNT(*)                                              AS TotalUrls,
                        SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END)       AS ActiveUrls,
                        COALESCE(SUM(CAST(TotalClicks AS BIGINT)), 0)        AS TotalClicks,
                        COUNT(DISTINCT CreatedByMemberId)                    AS MemberCount,
                        CASE WHEN COUNT(*) = 0 THEN 0.0
                             ELSE CAST(COALESCE(SUM(TotalClicks),0) AS FLOAT) / COUNT(*)
                        END                                                  AS AverageClicksPerUrl
                    FROM ShortUrls WITH (NOLOCK)
                    WHERE OrganizationId = {organizationId}
                    """)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
 
            return result ?? new OrganizationAnalyticsSummary();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving analytics for organization: {OrganizationId}", organizationId);
            throw new DatabaseException("Failed to retrieve organization analytics", ex);
        }
    }
}
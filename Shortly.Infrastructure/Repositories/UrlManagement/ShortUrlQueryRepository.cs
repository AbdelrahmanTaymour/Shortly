using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UrlManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UrlManagement;

public class ShortUrlQueryRepository(SQLServerDbContext dbContext, ILogger<ShortUrlQueryRepository> logger)
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
            return await dbContext.ShortUrls
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
    public async Task<IEnumerable<ShortUrl>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(s => s.UserId == userId, pageNumber, pageSize, cancellationToken);
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrl>> GetByOrganizationIdAsync(Guid organizationId, int pageNumber,
        int pageSize, CancellationToken cancellationToken = default)
    {
        return await SearchAsync(s => s.OrganizationId == organizationId, pageNumber, pageSize, cancellationToken);
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrl>> GetAnonymousUrlsByDateRangeAsync(DateTime startDate, DateTime endDate,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be greater than end date");

        return await SearchAsync(s => s.OwnerType == enShortUrlOwnerType.Anonymous &&
                                      s.CreatedAt >= startDate &&
                                      s.CreatedAt <= endDate,
            pageNumber, pageSize, cancellationToken);
    }

    
    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrl>> GetExpiredAsync(DateTime nowUtc, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(s => s.ExpiresAt != null && s.ExpiresAt <= nowUtc, pageNumber, pageSize,
            cancellationToken);
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrl>> GetPrivateLinksAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(s => s.UserId == userId && s.IsPrivate, pageNumber, pageSize, cancellationToken);
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrl>> GetByDateRangeAsync(DateTime startDate, DateTime endDate,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be greater than end date");

        return await SearchAsync(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate,
            pageNumber, pageSize, cancellationToken);
    }


    /// <inheritdoc />
    public async Task<IEnumerable<IGrouping<string, ShortUrl>>> GetDuplicateUrlsAsync(Guid? userId = null,
        Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.ShortUrls.AsNoTracking();

            if (userId.HasValue)
                query = query.Where(s => s.UserId == userId);

            if (organizationId.HasValue)
                query = query.Where(s => s.OrganizationId == organizationId);

            // Find OriginalUrls that have duplicates
            var duplicateKeys = await query
                .GroupBy(s => s.OriginalUrl)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            if (duplicateKeys.Count == 0)
                return [];

            // Fetch only duplicates
            var duplicates = await query
                .Where(s => duplicateKeys.Contains(s.OriginalUrl))
                .ToListAsync(cancellationToken);

            // Step 3: Group in memory and return
            return duplicates
                .GroupBy(s => s.OriginalUrl)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding duplicate URLs");
            throw new DatabaseException("Failed to find duplicate URLs", ex);
        }
    }

    
    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrl>> GetUnusedUrlsAsync(TimeSpan? olderThan = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        Expression<Func<ShortUrl, bool>> predicate = s => s.TotalClicks == 0;

        if (olderThan.HasValue)
        {
            var cutoff = DateTime.UtcNow.Subtract(olderThan.Value);
            predicate = s => s.TotalClicks == 0 && s.CreatedAt <= cutoff;
        }

        return await SearchAsync(predicate, pageNumber, pageSize, cancellationToken);
    }
}
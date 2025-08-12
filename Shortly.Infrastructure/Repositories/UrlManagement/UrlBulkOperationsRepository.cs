using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UrlManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UrlManagement;

public class UrlBulkOperationsRepository(
    SQLServerDbContext dbContext,
    ILogger<UrlBulkOperationsRepository> logger) : IUrlBulkOperationsRepository
{
    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkDeleteAsync(IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids == null)
            throw new ArgumentNullException(nameof(ids));

        if (ids.Count == 0)
            throw new ArgumentException("IDs collection cannot be empty", nameof(ids));

        var idsCount = ids.Count;
        logger.LogInformation("Starting bulk deletion for {IdsCount} short URLs", idsCount);

        try
        {
            var successCount = await dbContext.ShortUrls
                .Where(s => ids.Contains(s.Id))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            var skippedCount = idsCount - successCount;
            logger.LogInformation("Bulk deletion completed: {SuccessCount} deleted, {SkippedCount} skipped",
                successCount, skippedCount);

            return new BulkOperationResult(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk deletion of {TotalCount} short URLs", idsCount);
            throw new DatabaseException("Bulk deletion failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkDeactivateAsync(IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids == null)
            throw new ArgumentNullException(nameof(ids));

        if (ids.Count == 0)
            throw new ArgumentException("IDs collection cannot be empty", nameof(ids));

        var idsCount = ids.Count;
        logger.LogInformation("Starting bulk deactivation for {IdsCount} short URLs", idsCount);

        try
        {
            var now = DateTime.UtcNow;
            var successCount = await dbContext.ShortUrls
                .Where(s => ids.Contains(s.Id))
                .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsActive, false)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken)
                .ConfigureAwait(false);

            var skippedCount = idsCount - successCount;
            logger.LogInformation("Bulk deactivation completed: {SuccessCount} deactivated, {SkippedCount} skipped",
                successCount, skippedCount);

            return new BulkOperationResult(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk deactivation of {TotalCount} short URLs", idsCount);
            throw new DatabaseException("Bulk deactivation failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkActivateAsync(IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids == null)
            throw new ArgumentNullException(nameof(ids));

        if (ids.Count == 0)
            throw new ArgumentException("IDs collection cannot be empty", nameof(ids));

        var idsCount = ids.Count;
        logger.LogInformation("Starting bulk activation for {IdsCount} short URLs", idsCount);

        try
        {
            var now = DateTime.UtcNow;
            var successCount = await dbContext.ShortUrls
                .Where(s => ids.Contains(s.Id))
                .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsActive, true)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken)
                .ConfigureAwait(false);

            var skippedCount = idsCount - successCount;
            logger.LogInformation("Bulk activation completed: {SuccessCount} activated, {SkippedCount} skipped",
                successCount, skippedCount);

            return new BulkOperationResult(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk activation of {TotalCount} short URLs", idsCount);
            throw new DatabaseException("Bulk activation failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> DeleteExpiredAsync(DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting deletion of expired short URLs");

        try
        {
            var successCount = await dbContext.ShortUrls
                .Where(s => s.ExpiresAt != null && s.ExpiresAt <= nowUtc)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation("Deleted {SuccessCount} expired short URLs", successCount);
            return new BulkOperationResult(-1, successCount, -1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during expired URLs deletion");
            throw new DatabaseException("Failed to delete expired URLs", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkUpdateExpirationAsync(IReadOnlyCollection<long> ids,
        DateTime? newExpirationDate, CancellationToken cancellationToken = default)
    {
        var idsCount = ids.Count;
        logger.LogInformation("Starting bulk expiration update for {IdsCount} short URLs", idsCount);
        
        try
        {
            var now = DateTime.UtcNow;
            var successCount = await dbContext.ShortUrls
                .Where(s => ids.Contains(s.Id))
                .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.ExpiresAt, newExpirationDate)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken)
                .ConfigureAwait(false);

            var skippedCount = idsCount - successCount;
            logger.LogInformation("Bulk expiration update completed: {SuccessCount} updated, {SkippedCount} skipped",
                successCount, skippedCount);

            return new BulkOperationResult(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk expiration update of {TotalCount} short URLs", idsCount);
            throw new DatabaseException("Bulk expiration update failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkCreateAsync(IReadOnlyCollection<ShortUrl> shortUrls,
        CancellationToken cancellationToken = default)
    {
        var count = shortUrls.Count;
        logger.LogInformation("Starting bulk creation for {Count} short URLs", count);

        try
        {
            await dbContext.ShortUrls.AddRangeAsync(shortUrls, cancellationToken).ConfigureAwait(false);
            var affected = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Bulk creation completed: {Affected} short URLs created", affected);
            return new BulkOperationResult(count, affected, count - affected);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk creation of {Count} short URLs", count);
            throw new DatabaseException("Bulk creation failed", ex);
        }
    }

    
    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkUpdateShortCodeAsync(IReadOnlyDictionary<long, string?> shortUrlsMap,
        CancellationToken cancellationToken = default)
    {
        var idsCount = shortUrlsMap.Count;
        logger.LogInformation("Starting bulk expiration update for {IdsCount} short URLs", idsCount);
        
        try
        {
            var successCount = await dbContext.ShortUrls
                .Where(s => shortUrlsMap.Keys.Contains(s.Id))
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(s => s.ShortCode, s => shortUrlsMap[s.Id])
                        .SetProperty(s => s.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken)
                .ConfigureAwait(false);
        
            var skippedCount = idsCount - successCount;
            logger.LogInformation("Bulk short code update completed: {SuccessCount} updated, {SkippedCount} skipped",
                successCount, skippedCount);
        
            return new BulkOperationResult(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk short code update of {TotalCount} short URLs", idsCount);
            throw new DatabaseException("Bulk short code update failed", ex);
        }
    }
}
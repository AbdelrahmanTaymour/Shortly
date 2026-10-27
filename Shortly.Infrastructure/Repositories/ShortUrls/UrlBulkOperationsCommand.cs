using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Shortly.Core.Common;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.ShortUrls.Contracts;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.Repositories.ShortUrls;

public class UrlBulkOperationsCommand(
    ApplicationDbContext dbContext,
    ILogger<UrlBulkOperationsCommand> logger) : IUrlBulkOperationsCommand
{
    /// <inheritdoc />
    public async Task<BulkOperationResponse> BulkDeleteAsync(IReadOnlyCollection<long> ids,
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
            var successCount = await dbContext.Set<ShortUrl>()
                .Where(s => ids.Contains(s.Id))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            var skippedCount = idsCount - successCount;
            logger.LogInformation("Bulk deletion completed: {SuccessCount} deleted, {SkippedCount} skipped",
                successCount, skippedCount);

            return new BulkOperationResponse(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk deletion of {TotalCount} short URLs", idsCount);
            throw new DatabaseException("Bulk deletion failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResponse> BulkDeactivateAsync(IReadOnlyCollection<long> ids,
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
            var successCount = await dbContext.Set<ShortUrl>()
                .Where(s => ids.Contains(s.Id))
                .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsActive, false)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken)
                .ConfigureAwait(false);

            var skippedCount = idsCount - successCount;
            logger.LogInformation("Bulk deactivation completed: {SuccessCount} deactivated, {SkippedCount} skipped",
                successCount, skippedCount);

            return new BulkOperationResponse(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk deactivation of {TotalCount} short URLs", idsCount);
            throw new DatabaseException("Bulk deactivation failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResponse> BulkActivateAsync(IReadOnlyCollection<long> ids,
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
            var successCount = await dbContext.Set<ShortUrl>()
                .Where(s => ids.Contains(s.Id))
                .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsActive, true)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken)
                .ConfigureAwait(false);

            var skippedCount = idsCount - successCount;
            logger.LogInformation("Bulk activation completed: {SuccessCount} activated, {SkippedCount} skipped",
                successCount, skippedCount);

            return new BulkOperationResponse(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk activation of {TotalCount} short URLs", idsCount);
            throw new DatabaseException("Bulk activation failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResponse> DeleteExpiredAsync(DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting deletion of expired short URLs");

        try
        {
            var successCount = await dbContext.Set<ShortUrl>()
                .Where(s => s.ExpiresAt != null && s.ExpiresAt <= nowUtc)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation("Deleted {SuccessCount} expired short URLs", successCount);
            return new BulkOperationResponse(-1, successCount, -1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during expired URLs deletion");
            throw new DatabaseException("Failed to delete expired URLs", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResponse> BulkUpdateExpirationAsync(IReadOnlyCollection<long> ids,
        DateTime? newExpirationDate, CancellationToken cancellationToken = default)
    {
        var idsCount = ids.Count;
        logger.LogInformation("Starting bulk expiration update for {IdsCount} short URLs", idsCount);

        try
        {
            var now = DateTime.UtcNow;
            var successCount = await dbContext.Set<ShortUrl>()
                .Where(s => ids.Contains(s.Id))
                .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.ExpiresAt, newExpirationDate)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken)
                .ConfigureAwait(false);

            var skippedCount = idsCount - successCount;
            logger.LogInformation("Bulk expiration update completed: {SuccessCount} updated, {SkippedCount} skipped",
                successCount, skippedCount);

            return new BulkOperationResponse(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk expiration update of {TotalCount} short URLs", idsCount);
            throw new DatabaseException("Bulk expiration update failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResponse> BulkCreateAsync(IReadOnlyCollection<ShortUrl> shortUrls,
        CancellationToken cancellationToken = default)
    {
        var count = shortUrls.Count;
        logger.LogInformation("Starting bulk creation for {Count} short URLs", count);

        try
        {
            await dbContext.Set<ShortUrl>().AddRangeAsync(shortUrls, cancellationToken).ConfigureAwait(false);
            var affected = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Bulk creation completed: {Affected} short URLs created", affected);
            return new BulkOperationResponse(count, affected, count - affected);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk creation of {Count} short URLs", count);
            throw new DatabaseException("Bulk creation failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResponse> BulkUpdateShortCodeAsync(IReadOnlyDictionary<long, string?> shortUrlsMap,
        CancellationToken cancellationToken = default)
    {
        var idsCount = shortUrlsMap.Count;

        if (idsCount == 0)
            return new BulkOperationResponse(0, 0, 0);

        logger.LogInformation("Starting bulk short code update for {IdsCount} short URLs", idsCount);

        try
        {
            var validUpdates = shortUrlsMap
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .ToList();

            if (validUpdates.Count == 0)
            {
                logger.LogWarning("No valid short codes provided for update");
                return new BulkOperationResponse(idsCount, 0, idsCount);
            }

            var successCount = 0;
            const int batchSize = 500;

            for (var i = 0; i < validUpdates.Count; i += batchSize)
            {
                var batch = validUpdates.Skip(i).Take(batchSize).ToList();

                var sql = BuildUpdateSql(batch, out var parameters);
                var batchSuccessCount = await dbContext.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
                successCount += batchSuccessCount;
            }

            var skippedCount = idsCount - successCount;

            logger.LogInformation("Bulk short code update completed: {SuccessCount} updated, {SkippedCount} skipped",
                successCount, skippedCount);

            return new BulkOperationResponse(idsCount, successCount, skippedCount);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Bulk short code update was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk short code update of {TotalCount} short URLs", idsCount);
            throw new DatabaseException("Bulk short code update failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<HashSet<string?>> GetExistingCustomShortCodesAsync(IReadOnlyCollection<string> customCodes,
        CancellationToken cancellationToken)
    {
        try
        {
            var codes = await dbContext.Set<ShortUrl>()
                .Where(su => customCodes.Contains(su.ShortCode))
                .Select(su => su.ShortCode)
                .ToListAsync(cancellationToken);

            return [.. codes];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check existing custom short codes");
            throw new DatabaseException("Failed to validate custom short codes", ex);
        }
    }

    private static string BuildUpdateSql(List<KeyValuePair<long, string?>> batch, out object[] parameters)
    {
        var sqlBuilder = new StringBuilder();
        var paramList = new List<NpgsqlParameter>();

        // Assumes snake_case table and column names matching EFCore naming conventions
        sqlBuilder.AppendLine("UPDATE short_urls SET");
        sqlBuilder.AppendLine("    short_code = CASE id");

        var paramIndex = 0;
        foreach (var kvp in batch)
        {
            sqlBuilder.AppendLine($"        WHEN @p{paramIndex} THEN @p{paramIndex + 1}");
            paramList.Add(new NpgsqlParameter($"@p{paramIndex}", NpgsqlDbType.Bigint) { Value = kvp.Key });
            paramList.Add(new NpgsqlParameter($"@p{paramIndex + 1}", NpgsqlDbType.Text) { Value = (object?)kvp.Value ?? DBNull.Value });
            paramIndex += 2;
        }

        sqlBuilder.AppendLine("    END,");
        sqlBuilder.AppendLine($"    updated_at = @p{paramIndex}");
        paramList.Add(new NpgsqlParameter($"@p{paramIndex}", NpgsqlDbType.TimestampTz) { Value = DateTime.UtcNow });

        sqlBuilder.Append("WHERE id IN (");
        var idParams = batch.Select((_, idx) => $"@p{idx * 2}");
        sqlBuilder.Append(string.Join(", ", idParams));
        sqlBuilder.AppendLine(")");

        parameters = paramList.ToArray();
        return sqlBuilder.ToString();
    }
}
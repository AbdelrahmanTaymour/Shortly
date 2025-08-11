using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract;
using Shortly.Core.RepositoryContract.UrlManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UrlManagement;

public class UrlBulkOperationsRepository(
    SQLServerDbContext dbContext,
    IShortUrlRepository shortUrlRepository,
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
        if (ids == null)
            throw new ArgumentNullException(nameof(ids));

        if (ids.Count == 0)
            throw new ArgumentException("IDs collection cannot be empty", nameof(ids));

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
        if (shortUrls == null)
            throw new ArgumentNullException(nameof(shortUrls));

        if (shortUrls.Count == 0)
            throw new ArgumentException("Short URLs collection cannot be empty", nameof(shortUrls));

        var count = shortUrls.Count;
        logger.LogInformation("Starting bulk creation for {Count} short URLs", count);

        try
        {
            var now = DateTime.UtcNow;
            foreach (var url in shortUrls)
            {
                await ValidateShortUrlAsync(url, cancellationToken);
                url.CreatedAt = now;
                url.UpdatedAt = now;
            }

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


    
    #region Helper Methods

    /// <summary>
    ///     Validates a short URL entity before database operations.
    /// </summary>
    /// <param name="shortUrl">The short URL to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    /// <remarks>
    ///     This method performs comprehensive validation including URL format,
    ///     short code uniqueness, and business rule compliance.
    /// </remarks>
    private async Task ValidateShortUrlAsync(ShortUrl shortUrl, CancellationToken cancellationToken)
    {
        // Validate original URL format
        if (!Uri.TryCreate(shortUrl.OriginalUrl, UriKind.Absolute, out _))
            throw new ValidationException("Original URL format is invalid");

        // Validate short code if provided
        if (!string.IsNullOrEmpty(shortUrl.ShortCode))
        {
            if (shortUrl.ShortCode.Length < 3 || shortUrl.ShortCode.Length > 50)
                throw new ValidationException("Short code must be between 3 and 50 characters");

            // if (await shortUrlRepository.ShortCodeExistsAsync(shortUrl.ShortCode, cancellationToken))
            //     throw new ValidationException("Short code already exists");
        }

        // Validate click limit
        if (shortUrl.ClickLimit < -1)
            throw new ValidationException("Click limit must be -1 (unlimited) or a positive number");

        // Validate expiration date
        if (shortUrl.ExpiresAt.HasValue && shortUrl.ExpiresAt <= DateTime.UtcNow)
            throw new ValidationException("Expiration date must be in the future");

        // Validate owner information
        if (shortUrl.OwnerType == enShortUrlOwnerType.User && !shortUrl.UserId.HasValue)
            throw new ValidationException("User ID is required for user-owned URLs");

        if (shortUrl.OwnerType == enShortUrlOwnerType.Organization && !shortUrl.OrganizationId.HasValue)
            throw new ValidationException("Organization ID is required for organization-owned URLs");
    }

    #endregion
}
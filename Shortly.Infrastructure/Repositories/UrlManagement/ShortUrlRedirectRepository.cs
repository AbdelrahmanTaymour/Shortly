using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UrlManagement;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UrlManagement;

public class ShortUrlRedirectRepository(SQLServerDbContext dbContext, ILogger<ShortUrlRedirectRepository> logger)
    : IShortUrlRedirectRepository
{
    /// <inheritdoc />
    public async Task<ShortUrlRedirectInfoDto?> GetRedirectInfoByShortCodeAsync(string shortCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            throw new ArgumentException("Short code cannot be null or empty", nameof(shortCode));

        try
        {
            var now = DateTime.UtcNow;
            return await dbContext.ShortUrls
                .AsNoTracking()
                .Where(s => s.ShortCode == shortCode && s.IsActive && (s.ExpiresAt == null || s.ExpiresAt > now))
                .Select(s => new ShortUrlRedirectInfoDto
                {
                    Id = s.Id,
                    ShortCode = s.ShortCode,
                    OriginalUrl = s.OriginalUrl,
                    IsActive = s.IsActive,
                    IsPasswordProtected = s.IsPasswordProtected,
                    PasswordHash = s.PasswordHash,
                    ClickLimit = s.ClickLimit,
                    TotalClicks = s.TotalClicks,
                    ExpiresAt = s.ExpiresAt,
                    OwnerType = s.OwnerType,
                    UserId = s.UserId,
                    OrganizationId = s.OrganizationId
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving redirect info for code: {ShortCode}", shortCode);
            throw new DatabaseException("Failed to retrieve redirect information", ex);
        }
    }

    
    /// <inheritdoc />
    public async Task<bool> IncrementClickCountAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var affected = await dbContext.ShortUrls
                .Where(s => s.ShortCode == shortCode && s.IsActive && (s.ExpiresAt == null || s.ExpiresAt > now))
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(s => s.TotalClicks, s => s.TotalClicks + 1)
                        .SetProperty(s => s.UpdatedAt, now),
                    cancellationToken)
                .ConfigureAwait(false);

            if (affected > 0)
                logger.LogDebug("Incremented click count for short code: {ShortCode}", shortCode);

            return affected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing click count for code: {ShortCode}", shortCode);
            throw new DatabaseException("Failed to increment click count", ex);
        }
    }

    
    /// <inheritdoc />
    public async Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await dbContext.ShortUrls
                .AsNoTracking()
                .AnyAsync(s => s.ShortCode == shortCode &&
                               (s.ExpiresAt == null || s.ExpiresAt > now),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking short code existence: {ShortCode}", shortCode);
            throw new DatabaseException("Failed to check short code existence", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsClickLimitReachedAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = await dbContext.ShortUrls
                .AsNoTracking()
                .Where(s => s.Id == shortUrlId)
                .Select(s => new { s.ClickLimit, s.TotalClicks })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return url != null && url.ClickLimit != -1 && url.TotalClicks >= url.ClickLimit;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking click limit for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to check click limit", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsPasswordProtectedAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ShortUrls
                .AsNoTracking()
                .AnyAsync(s => s.Id == shortUrlId && s.IsPasswordProtected, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking password protection for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to check password protection", ex);
        }
    }


    /// <inheritdoc />
    public async Task<bool> VerifyPasswordAsync(long shortUrlId, string passwordHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ShortUrls
                .AsNoTracking()
                .AnyAsync(s => s.Id == shortUrlId && s.PasswordHash == passwordHash, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying password for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to verify password", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetUrlIfPasswordCorrectAsync(string shortCode, string passwordHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ShortUrls
                .AsNoTracking()
                .Where(s => s.ShortCode == shortCode && s.PasswordHash == passwordHash)
                .Select(s => s.OriginalUrl)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error verifying password for ShortCode: {ShortCode}, Hash: {PasswordHash}",
                shortCode, passwordHash);

            throw new DatabaseException("Failed to verify password for the provided short code.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsValidAsync(long shortUrlId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ShortUrls
                .AsNoTracking()
                .AnyAsync(s => s.Id == shortUrlId &&
                               (s.ExpiresAt == null || s.ExpiresAt > nowUtc)
                    , cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking URL validity for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to check URL validity", ex);
        }
    }

    
    /// <inheritdoc />
    public async Task<bool> IsActiveAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await dbContext.ShortUrls
                .AsNoTracking()
                .AnyAsync(s => s.Id == shortUrlId && s.IsActive &&
                               (s.ExpiresAt == null || s.ExpiresAt > now)
                    , cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking URL active status for URL ID: {ShortUrlId}", shortUrlId);
            throw new DatabaseException("Failed to check URL active status", ex);
        }
    }
}
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UrlManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UrlManagement;

public class ShortUrlAnalyticsRepository(
    SQLServerDbContext dbContext,
    ILogger<ShortUrlAnalyticsRepository> logger,
    IShortUrlQueryRepository urlQueryRepository)
    : IShortUrlAnalyticsRepository
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
    public async Task<IReadOnlyList<ShortUrl>> GetMostPopularAsync(int topCount = 10, TimeSpan? timeframe = null,
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
    public async Task<UserAnalyticsSummary> GetUserAnalyticsAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await dbContext.ShortUrls
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .GroupBy(s => 1)
                .Select(g => new UserAnalyticsSummary
                {
                    TotalUrls = g.Count(),
                    ActiveUrls = g.Count(s => s.IsActive),
                    TotalClicks = g.Sum(s => s.TotalClicks),
                    PrivateUrls = g.Count(s => s.IsPrivate),
                    PasswordProtectedUrls = g.Count(s => s.IsPasswordProtected),
                    ExpiredUrls = g.Count(s => s.ExpiresAt != null && s.ExpiresAt <= DateTime.UtcNow)
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return summary ?? new UserAnalyticsSummary();
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
            var summary = await dbContext.ShortUrls
                .AsNoTracking()
                .Where(s => s.OrganizationId == organizationId)
                .GroupBy(s => 1)
                .Select(g => new OrganizationAnalyticsSummary
                {
                    TotalUrls = g.Count(),
                    ActiveUrls = g.Count(s => s.IsActive),
                    TotalClicks = g.Sum(s => s.TotalClicks),
                    MemberCount = g.Select(s => s.CreatedByMemberId).Distinct().Count(),
                    AverageClicksPerUrl = g.Average(s => (double)s.TotalClicks)
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return summary ?? new OrganizationAnalyticsSummary();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving analytics for organization: {OrganizationId}", organizationId);
            throw new DatabaseException("Failed to retrieve organization analytics", ex);
        }
    }


    /// <inheritdoc />
    public async Task<IReadOnlyList<ShortUrl>> GetApproachingLimitAsync(double warningThreshold = 0.8,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (warningThreshold is < 0.0 or > 1.0)
            throw new ArgumentException("Warning threshold must be between 0.0 and 1.0", nameof(warningThreshold));

        return await urlQueryRepository.SearchAsync(s => s.ClickLimit > 0 &&
                                                         s.TotalClicks >= s.ClickLimit * warningThreshold,
            pageNumber, pageSize, cancellationToken);
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
            if (shortUrl.ShortCode.Length < 3 || shortUrl.ShortCode.Length > 50)
                throw new ValidationException("Short code must be between 3 and 50 characters");
        // if (await ShortCodeExistsAsync(shortUrl.ShortCode, cancellationToken))
        //     throw new ValidationException("Short code already exists");
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UserManagement;

/// <summary>
/// SQL Server implementation of the user usage repository.
/// </summary>
/// <remarks>
/// Uses bulk update operations (ExecuteUpdateAsync) for optimal performance when incrementing usage counters.
/// Designed for high-frequency operations like tracking link and QR code creation.
/// </remarks>
public class UserUsageRepository(SQLServerDbContext dbContext, ILogger<UserUsageRepository> logger)
    : IUserUsageRepository
{
    /// <inheritdoc/>
    public async Task<UserUsage?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserUsage
                .AsNoTracking()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user usage for user ID: {UserId}", userId);
            throw new DatabaseException("Failed to retrieve user usage", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(UserUsage usage, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.UserUsage.Update(usage);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user usage for user ID: {UserId}", usage.UserId);
            throw new DatabaseException("Failed to update user usage", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IncrementLinksCreatedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rawAffected = await dbContext.UserUsage
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.MonthlyLinksCreated, u => u.MonthlyLinksCreated + 1)
                        .SetProperty(u => u.TotalLinksCreated, u => u.TotalLinksCreated + 1)
                    , cancellationToken);
            return rawAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing links created for user ID: {UserId}", userId);
            throw new DatabaseException("Failed to increment links created", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IncrementQrCodesCreatedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rawAffected = await dbContext.UserUsage
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.MonthlyQrCodesCreated, u => u.MonthlyQrCodesCreated + 1)
                        .SetProperty(u => u.TotalQrCodesCreated, u => u.TotalQrCodesCreated + 1)
                    , cancellationToken);
            return rawAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing QR codes created for user ID: {UserId}", userId);
            throw new DatabaseException("Failed to increment QR codes created", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ResetMonthlyUsageAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rawAffected = await dbContext.UserUsage
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.MonthlyLinksCreated, 0)
                        .SetProperty(u => u.MonthlyQrCodesCreated, 0)
                    , cancellationToken);
            return rawAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting monthly usage for user ID: {UserId}", userId);
            throw new DatabaseException("Failed to reset monthly usage", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<int> ResetMonthlyUsageForAllAsync(DateTime resetDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var affectedRows = await (
                    from usage in dbContext.UserUsage
                    join user in dbContext.Users on usage.UserId equals user.Id
                    where !user.IsDeleted && usage.MonthlyResetDate < resetDate
                    select usage
                )
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.MonthlyLinksCreated, 0)
                        .SetProperty(u => u.MonthlyQrCodesCreated, 0)
                        .SetProperty(u => u.MonthlyResetDate, DateTime.UtcNow.AddMonths(1)),
                    cancellationToken);

            logger.LogInformation("Monthly usage reset for {Count} users on {ResetDate}.", affectedRows,
                resetDate.Date);
            return affectedRows;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while resetting monthly usage before {ResetDate}.", resetDate.Date);
            throw new DatabaseException("Failed to reset monthly usage.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserUsage>> GetUsersWithResetDateInRangeAsync(DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserUsage
                .AsNoTracking()
                .Where(uu =>
                    uu.MonthlyResetDate.Date >= from.Date &&
                    uu.MonthlyResetDate.Date <= to.Date)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user usage report between {From} and {To}.", from, to);
            throw new DatabaseException("Failed to retrieve user usage report.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<UserUsageWithPlan?> GetUserUsageWithPlanIdAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await dbContext.Users
                .Where(u => u.Id == userId && u.IsActive && !u.IsDeleted)
                .Select(u => new UserUsageWithPlan
                (
                    u.SubscriptionPlanId,
                    u.UserUsage.MonthlyLinksCreated,
                    u.UserUsage.MonthlyQrCodesCreated,
                    u.UserUsage.MonthlyResetDate,
                    u.UserUsage.TotalLinksCreated,
                    u.UserUsage.TotalQrCodesCreated
                ))
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve usage and subscription plan for user: {UserId}", userId);
            throw new DatabaseException("Error retrieving user usage and subscription plan.", ex);
        }
    }
}
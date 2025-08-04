using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UserManagement;

public class UserUsageRepository(SQLServerDbContext dbContext, ILogger<UserUsageRepository> logger) : IUserUsageRepository
{
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

    public async Task<IEnumerable<UserUsage>> GetUsersForMonthlyResetAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserUsage
                .AsNoTracking()
                .Where(uu => uu.MonthlyResetDate.Date <= date.Date.AddDays(1)) // Include the entire day
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving users for monthly reset on date: {Date}", date);
            throw new DatabaseException("Failed to retrieve users for monthly reset", ex);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
public class UserUsageRepository(SQLServerDbContext dbContext, ILogger<UserUsageRepository> logger) : IUserUsageRepository
{
    /// <inheritdoc/>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses AsNoTracking for optimal read-only performance.</remarks>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance atomic increment of both monthly and total counters.</remarks>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance atomic increment of both monthly and total counters.</remarks>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance reset operation, typically called during billing cycles.</remarks>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Uses date comparison with AddDays(1) to include the entire target day for comprehensive batch processing.
    /// Designed for background service consumption.
    /// </remarks>
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
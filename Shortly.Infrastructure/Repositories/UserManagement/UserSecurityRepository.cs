using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UserManagement;

/// <summary>
/// SQL Server implementation of the user security repository.
/// </summary>
/// <remarks>
/// Uses bulk update operations (ExecuteUpdateAsync) for optimal performance when modifying security counters and lock states.
/// All operations update the UpdatedAt timestamp for audit tracking.
/// </remarks>
public class UserSecurityRepository(SQLServerDbContext dbContext, ILogger<UserSecurityRepository> logger) : IUserSecurityRepository
{
    /// <inheritdoc/>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses AsNoTracking for optimal read-only performance.</remarks>
    public async Task<UserSecurity?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserSecurity
                .AsNoTracking()
                .FirstOrDefaultAsync(us => us.UserId == userId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user security for user ID: {UserId}", userId);
            throw new DatabaseException("Failed to retrieve user security", ex);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    public async Task<bool> UpdateAsync(UserSecurity security, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.UserSecurity.Update(security);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user security for user ID: {UserId}", security.UserId);
            throw new DatabaseException("Failed to update user security", ex);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance atomic increment without loading entity into memory.</remarks>
    public async Task<bool> IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowAffected = await dbContext.UserSecurity
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(us => us.FailedLoginAttempts, us => us.FailedLoginAttempts + 1)
                        .SetProperty(us => us.UpdatedAt, us => DateTime.UtcNow)
                    , cancellationToken);
            return rowAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing failed login attempts for user ID: {UserId}", userId);
            throw new DatabaseException("Failed to increment failed login attempts", ex);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance atomic reset without loading entity into memory.</remarks>
    public async Task<bool> ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowAffected = await dbContext.UserSecurity
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(us => us.FailedLoginAttempts, us => 0)
                        .SetProperty(us => us.UpdatedAt, us => DateTime.UtcNow)
                    , cancellationToken);
            return rowAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting failed login attempts for user ID: {UserId}", userId);
            throw new DatabaseException("Failed to reset failed login attempts", ex);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance lock operation without loading entity into memory.</remarks>
    public async Task<bool> LockUserAsync(Guid userId, DateTime lockedUntil, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowAffected = await dbContext.UserSecurity
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.LockedUntil, lockedUntil)
                        .SetProperty(u => u.UpdatedAt, u => DateTime.UtcNow)
                    , cancellationToken);
            return rowAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error locking user ID: {UserId}", userId);
            throw new DatabaseException("Failed to lock user", ex);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Uses ExecuteUpdateAsync for high-performance unlock operation that clears the lock date and resets failed attempts.
    /// Performs comprehensive unlock in a single atomic operation.
    /// </remarks>
    public async Task<bool> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowAffected = await dbContext.UserSecurity
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.LockedUntil, (DateTime?)null)
                        .SetProperty(u => u.FailedLoginAttempts, 0)
                        .SetProperty(u => u.UpdatedAt, u => DateTime.UtcNow)
                    , cancellationToken);
            return rowAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unlocking user ID: {UserId}", userId);
            throw new DatabaseException("Failed to unlock user", ex);
        }
    }
}
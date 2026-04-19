using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.Users;

/// <summary>
///     SQL Server implementation of the user repository.
/// </summary>
/// <remarks>
///     Implements comprehensive user management with soft delete pattern, bulk operations for performance,
///     and transactional user creation with related entities.
/// </remarks>
public class UserRepository(SqlServerDbContext dbContext, ILogger<UserRepository> logger) : IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users.FindAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by ID: {UserId}", id);
            throw new DatabaseException("Failed to retrieve user", ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>Uses Entity Framework's Find method - may return deleted users as it doesn't filter by IsDeleted.</remarks>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.Equals(email) && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            throw new DatabaseException("Failed to retrieve user by email", ex);
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.Equals(username) && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by username: {Username}", username);
            throw new DatabaseException("Failed to retrieve user by username", ex);
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.Equals(emailOrUsername) ||
                                          (u.Username.Equals(emailOrUsername) && !u.IsDeleted),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by email or username: {emailOrUsername}", emailOrUsername);
            throw new DatabaseException("Failed to retrieve user by email or username", ex);
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.GoogleId == googleId && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by their Google ID: {googleId}", googleId);
            throw new DatabaseException("Failed to retrieve user by their Google ID", ex);
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetWithProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user with profile: {UserId}", id);
            throw new DatabaseException("Failed to retrieve user with profile", ex);
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetWithSecurityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Include(u => u.UserSecurity)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user with security: {UserId}", id);
            throw new DatabaseException("Failed to retrieve user with security", ex);
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetWithUsageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Include(u => u.UserUsage)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user with usage: {UserId}", id);
            throw new DatabaseException("Failed to retrieve user with usage", ex);
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetCompleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .Include(u => u.UserSecurity)
                .Include(u => u.UserUsage)
                .Include(u => u.SubscriptionPlan)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving complete user: {UserId}", id);
            throw new DatabaseException("Failed to retrieve complete user", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<enSubscriptionPlan> GetSubscriptionPlanIdAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted)
                .Select(u => u.SubscriptionPlanId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user Subscription Plan: {UserId}", id);
            throw new DatabaseException("Failed to retrieve user Subscription Plan", ex);
        }
    }

    /// <inheritdoc />
    public async Task<User> CreateAsync(User user)
    {
        if (user.Id == Guid.Empty)
            throw new ValidationException("UserId cannot be empty");

        // Create the execution strategy
        var strategy = dbContext.Database.CreateExecutionStrategy();

        // Wrap the logic in ExecuteAsync
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var relatedEntities = new object[]
                {
                    user,
                    new UserProfile { UserId = user.Id },
                    new UserSecurity { UserId = user.Id },
                    new UserUsage { UserId = user.Id }
                };

                await dbContext.AddRangeAsync(relatedEntities);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return user;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Error creating user: {Email}", user.Email);
                throw new DatabaseException("Failed to create user", ex);
            }
        });
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(User user)
    {
        try
        {
            dbContext.Update(user);
            return await dbContext.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user: {UserId}", user.Id);
            throw new DatabaseException("Failed to update user", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ChangeEmailAsync(Guid id, string newEmail, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.Email, newEmail)
                        .SetProperty(u => u.IsEmailConfirmed, true)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing user email: {UserId}", id);
            throw new DatabaseException("Failed to change user email.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(Guid id, string newPasswordHash, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.PasswordHash, newPasswordHash)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing user password: {UserId}", id);
            throw new DatabaseException("Failed to change user password.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var rawAffected = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.IsDeleted, true)
                        .SetProperty(u => u.IsActive, false)
                        .SetProperty(u => u.DeletedAt, DateTime.UtcNow)
                        .SetProperty(u => u.DeletedBy, deletedBy)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken);
            return rawAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user: {UserId}", id);
            throw new DatabaseException("Failed to delete user", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ActivateUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted && !u.IsActive)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.IsActive, true)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error activate user: {UserId}", id);
            throw new DatabaseException("Failed to activate user", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted && u.IsActive)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.IsActive, false)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deactivate user: {UserId}", id);
            throw new DatabaseException("Failed to deactivate user", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkEmailAsConfirmedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted && !u.IsEmailConfirmed)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.IsEmailConfirmed, true)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verify user Email: {UserId}", id);
            throw new DatabaseException("Failed to verify user Email", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user exists: {UserId}", id);
            throw new DatabaseException("Failed to check user existence", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email.Equals(email), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if email exists: {Email}", email);
            throw new DatabaseException("Failed to check email existence", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Username.Equals(username), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if username exists: {Username}", username);
            throw new DatabaseException("Failed to check username existence", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> EmailOrUsernameExistsAsync(string email, string username,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => (u.Username.Equals(username) ||
                                u.Email.Equals(email))
                    , cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if email or username exists: {email}, {username}", email, username);
            throw new DatabaseException("Failed to check email or username existence", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            throw new ValidationException("Page must be greater than 0");

        if (pageSize is < 1 or > 1000)
            throw new ValidationException("Page size must be between 1 and 1000");

        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paged users: Page {Page}, PageSize {PageSize}", page, pageSize);
            throw new DatabaseException("Failed to retrieve paged users", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetUsersByCustomCriteriaAsync(Expression<Func<User, bool>> predicateint,
        int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .Where(predicateint)
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving paged users: Custom Criteria {CustomCriteria}, Page {Page}, PageSize {PageSize}",
                predicateint, page, pageSize);
            throw new DatabaseException("Failed to retrieve paged users", ex);
        }
    }
    
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UserManagement;

public class UserRepository(SQLServerDbContext dbContext, ILogger<UserRepository> logger) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        try
        {
            return await dbContext.Users.FindAsync(id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by ID: {UserId}", id);
            throw new DatabaseException("Failed to retrieve user", ex);
        }
    }

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

    public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.Equals(emailOrUsername) || 
                                          u.Username.Equals(emailOrUsername) && !u.IsDeleted, 
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by email or username: {emailOrUsername}", emailOrUsername);
            throw new DatabaseException("Failed to retrieve user by email or username", ex);
        }
    }

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

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        if(user.Id == Guid.Empty)
            throw new ValidationException("UserId cannot be empty");
                
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Bulk insert related entities
            var relatedEntities = new object[]
            {
                user,
                new UserProfile { UserId = user.Id },
                new UserSecurity { UserId = user.Id },
                new UserUsage { UserId = user.Id }
            };
        
            await dbContext.AddRangeAsync(relatedEntities, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        
            return user;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error creating user: {Email}", user.Email);
            throw new DatabaseException("Failed to create user", ex);
        }
    }

    public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.Update(user);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user: {UserId}", user.Id);
            throw new DatabaseException("Failed to update user", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var rawAffected = await dbContext.Users
                .Where(u => u.Id == id)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.IsDeleted, true)
                        .SetProperty(u => u.DeletedAt, DateTime.UtcNow)
                        .SetProperty(u => u.DeletedBy, deletedBy)
                        .SetProperty(u=> u.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken);
            return rawAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user: {UserId}", id);
            throw new DatabaseException("Failed to delete user", ex);
        }
    }

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

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email.Equals(email) && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if email exists: {Email}", email);
            throw new DatabaseException("Failed to check email existence", ex);
        }
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Username.Equals(username) && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if username exists: {Username}", username);
            throw new DatabaseException("Failed to check username existence", ex);
        }
    }

    public async Task<bool> EmailOrUsernameExistsAsync(string email, string username, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => (u.Username.Equals(username) || 
                                u.Email.Equals(email)) && !u.IsDeleted
                , cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if email or username exists: {email}, {username}", email, username);
            throw new DatabaseException("Failed to check email or username existence", ex);
        }
    }

    public async Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
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
}
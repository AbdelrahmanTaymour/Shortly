using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UsersDTOs.Profile;
using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.DTOs.UsersDTOs.Security;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UserManagement;

/// <summary>
///     SQL Server implementation of the user repository.
/// </summary>
/// <remarks>
///     Implements comprehensive user management with soft delete pattern, bulk operations for performance,
///     and transactional user creation with related entities.
/// </remarks>
public class UserRepository(SQLServerDbContext dbContext, ILogger<UserRepository> logger) : IUserRepository
{
    /// <inheritdoc />
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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

    /// <inheritdoc />
    /// <remarks>Uses Entity Framework's Find method - may return deleted users as it doesn't filter by IsDeleted.</remarks>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses eager loading with Include to fetch profile data in a single query.</remarks>
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
    /// <remarks>Uses eager loading with Include to fetch security data in a single query.</remarks>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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
    /// <remarks>Uses eager loading with Include to fetch usage data in a single query.</remarks>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Loads all related entities in a single query - use carefully as this can be expensive for large datasets.</remarks>
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

    /// <inheritdoc />
    /// <exception cref="ValidationException">Thrown when user ID is empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    ///     Creates user and all related entities (UserProfile, UserSecurity, UserUsage) in a single transaction.
    ///     Uses bulk insert with AddRangeAsync for optimal performance.
    /// </remarks>
    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user.Id == Guid.Empty)
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

    /// <inheritdoc />
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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

    /// <inheritdoc />
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance bulk update without loading entity into memory.</remarks>
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

    /// <inheritdoc />
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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

    /// <inheritdoc />
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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

    /// <inheritdoc />
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    public async Task<bool> EmailOrUsernameExistsAsync(string email, string username,
        CancellationToken cancellationToken = default)
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

    /// <inheritdoc />
    /// <exception cref="ValidationException">Thrown when page or pageSize parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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
    /// <remarks>
    /// Results are ordered by <c>CreatedAt</c> in ascending order.
    /// </remarks>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    public async Task<IEnumerable<User>> GetUsersByCustomCriteriaAsync(Expression<Func<User, bool>> predicateint, int page = 1, int pageSize = 10,
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


    /// <inheritdoc />
    /// <exception cref="ValidationException">
    ///     Thrown when <paramref name="page" /> or <paramref name="pageSize" /> are outside
    ///     valid bounds.
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    public async Task<(IEnumerable<IUserSearchResult> Users, int TotalCount)> SearchUsers(
        string? searchTerm = null,
        enSubscriptionPlan? subscriptionPlan = null,
        bool? isActive = null,
        bool? isDeleted = null,
        bool? isEmailConfirmed = null, 
        int page = 1,
        int pageSize = 10,
        bool retrieveCompleteUser = false,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            throw new ValidationException("Page must be greater than 0");

        if (pageSize is < 1 or > 1000)
            throw new ValidationException("Page size must be between 1 and 1000");

        try
        {
            var query = dbContext.Users
                .AsNoTracking()
                .AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(u =>
                    u.Email.Contains(searchTerm) ||
                    u.Username.Contains(searchTerm));

            if (subscriptionPlan.HasValue)
                query = query.Where(u => u.SubscriptionPlanId == subscriptionPlan.Value);

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            if (isDeleted.HasValue)
                query = query.Where(u => u.IsDeleted == isDeleted.Value);

            if (isEmailConfirmed.HasValue)
                query = query.Where(u => u.IsEmailConfirmed == isEmailConfirmed.Value);

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Include related data if full details are requested
            if (retrieveCompleteUser)
            {
                var detailedUsers = await query
                    .Include(u => u.Profile)
                    .Include(u => u.UserSecurity)
                    .Include(u => u.UserUsage)
                    .OrderBy(u => u.Username)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new CompleteUserSearchResult(
                        u.Id,
                        u.Email,
                        u.Username,
                        u.SubscriptionPlanId,
                        u.Permissions,
                        u.IsActive,
                        u.IsEmailConfirmed,
                        u.UpdatedAt,
                        u.CreatedAt,
                        u.IsDeleted,
                        u.DeletedAt,
                        u.DeletedBy,
                        new UserProfileDto(
                            u.Profile.Name,
                            u.Profile.Bio,
                            u.Profile.PhoneNumber,
                            u.Profile.ProfilePictureUrl,
                            u.Profile.Website,
                            u.Profile.Company,
                            u.Profile.Location,
                            u.Profile.Country,
                            u.Profile.TimeZone,
                            u.Profile.UpdatedAt
                        ),
                        new UserSecurityDto(
                            u.UserSecurity.FailedLoginAttempts,
                            u.UserSecurity.LockedUntil,
                            u.UserSecurity.TwoFactorEnabled,
                            u.UserSecurity.TwoFactorSecret,
                            u.UserSecurity.PasswordResetToken,
                            u.UserSecurity.TokenExpiresAt,
                            u.UserSecurity.UpdatedAt
                        ),
                        new UserUsageDto(
                            u.UserUsage.UserId,
                            u.UserUsage.MonthlyLinksCreated,
                            u.UserUsage.MonthlyQrCodesCreated,
                            u.UserUsage.TotalLinksCreated,
                            u.UserUsage.TotalQrCodesCreated,
                            u.UserUsage.MonthlyResetDate
                        )
                    ))
                    .Cast<IUserSearchResult>()
                    .ToListAsync(cancellationToken);

                return (detailedUsers, detailedUsers.Count);
            }

            var basicUsers = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserSearchResult(
                    u.Id,
                    u.Email,
                    u.Username,
                    u.SubscriptionPlanId,
                    u.IsActive,
                    u.Permissions
                ))
                .Cast<IUserSearchResult>()
                .ToListAsync(cancellationToken);

            return (basicUsers, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paged users: Page {Page}, PageSize {PageSize}", page, pageSize);
            throw new DatabaseException("Failed to retrieve paged users", ex);
        }
    }
}
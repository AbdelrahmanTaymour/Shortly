using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Mappers;
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
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
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
    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var rawAffected = await dbContext.Users
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


    /// <inheritdoc />
    public async Task<(IEnumerable<IUserSearchResult> Users, int TotalCount)> SearchUsers(
        UserSearchRequest request, bool retrieveCompleteUser = false, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        try
        {
            var query = BuildUserQuery(request);
            var totalCount = await query.CountAsync(cancellationToken);

            var users = retrieveCompleteUser
                ? await GetCompleteUsers(query, request, cancellationToken)
                : await GetBasicUsers(query, request, cancellationToken);

            return (users, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paged users: Page {Page}, PageSize {PageSize}", request.Page,
                request.PageSize);
            throw new DatabaseException("Failed to retrieve paged users", ex);
        }
    }


    #region Private Healper Method

    /// <summary>
    /// Validates the search request parameters to ensure they meet business requirements.
    /// </summary>
    /// <param name="request">The user search request to validate.</param>
    /// <exception cref="ValidationException">Thrown when page is less than 1 or pageSize is not between 1 and 1000.</exception>
    private static void ValidateRequest(UserSearchRequest request)
    {
        if (request.Page < 1)
            throw new ValidationException("Page must be greater than 0");

        if (request.PageSize is < 1 or > 1000)
            throw new ValidationException("Page size must be between 1 and 1000");
    }


    /// <summary>
    /// Builds a filtered and ordered query for users based on the search criteria.
    /// </summary>
    /// <param name="request">The search request containing filter criteria.</param>
    /// <returns>An IQueryable of User entities with applied filters and ordering by username.</returns>
    private IQueryable<User> BuildUserQuery(UserSearchRequest request)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .AsQueryable();

        query = ApplySearchTermFilter(query, request.SearchTerm);
        query = ApplySubscriptionPlanFilter(query, request.SubscriptionPlan);
        query = ApplyBooleanFilters(query, request);

        return query.OrderBy(u => u.Username);
    }


    /// <summary>
    /// Applies search term filtering to the user query, searching across email and username fields.
    /// </summary>
    /// <param name="query">The base user query to filter.</param>
    /// <param name="searchTerm">The optional search term to match against user fields.</param>
    /// <returns>The filtered query if searchTerm is provided; otherwise, the original query.</returns>
    private static IQueryable<User> ApplySearchTermFilter(IQueryable<User> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.Where(u =>
            u.Email.Contains(searchTerm) ||
            u.Username.Contains(searchTerm));
    }


    /// <summary>
    /// Applies subscription plan filtering to the user query.
    /// </summary>
    /// <param name="query">The base user query to filter.</param>
    /// <param name="subscriptionPlan">The optional subscription plan to filter by.</param>
    /// <returns>The filtered query if subscriptionPlan is provided; otherwise, the original query.</returns>
    private static IQueryable<User> ApplySubscriptionPlanFilter(IQueryable<User> query,
        enSubscriptionPlan? subscriptionPlan)
    {
        if (!subscriptionPlan.HasValue)
            return query;

        return query.Where(u => u.SubscriptionPlanId == subscriptionPlan.Value);
    }


    /// <summary>
    /// Applies boolean filters (IsActive, IsDeleted, IsEmailConfirmed) to the user query.
    /// </summary>
    /// <param name="query">The base user query to filter.</param>
    /// <param name="request">The search request containing boolean filter values.</param>
    /// <returns>The query with applied boolean filters.</returns>
    private static IQueryable<User> ApplyBooleanFilters(IQueryable<User> query, UserSearchRequest request)
    {
        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        if (request.IsDeleted.HasValue)
            query = query.Where(u => u.IsDeleted == request.IsDeleted.Value);

        if (request.IsEmailConfirmed.HasValue)
            query = query.Where(u => u.IsEmailConfirmed == request.IsEmailConfirmed.Value);

        return query;
    }


    /// <summary>
    /// Retrieves complete user information including related entities (Profile, UserSecurity, UserUsage) with pagination.
    /// </summary>
    /// <param name="query">The pre-filtered and ordered user query.</param>
    /// <param name="request">The search request containing pagination parameters.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A collection of complete user search results with all related data.</returns>
    private async Task<IEnumerable<IUserSearchResult>> GetCompleteUsers(
        IQueryable<User> query,
        UserSearchRequest request,
        CancellationToken cancellationToken)
    {
        var users = await query
            .Include(u => u.Profile)
            .Include(u => u.UserSecurity)
            .Include(u => u.UserUsage)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => CreateCompleteUserSearchResult(u))
            .Cast<IUserSearchResult>()
            .ToListAsync(cancellationToken);

        return users;
    }


    /// <summary>
    /// Retrieves basic user information without related entities, with pagination.
    /// </summary>
    /// <param name="query">The pre-filtered and ordered user query.</param>
    /// <param name="request">The search request containing pagination parameters.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A collection of basic user search results containing essential user data only.</returns>
    private async Task<IEnumerable<IUserSearchResult>> GetBasicUsers(
        IQueryable<User> query,
        UserSearchRequest request,
        CancellationToken cancellationToken)
    {
        var users = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => CreateBasicUserSearchResult(u))
            .Cast<IUserSearchResult>()
            .ToListAsync(cancellationToken);

        return users;
    }


    /// <summary>
    /// Creates a complete user search result with all related entities populated.
    /// </summary>
    /// <param name="u">The user entity with related data loaded.</param>
    /// <returns>A CompleteUserSearchResult containing user data and all related DTOs.</returns>
    private static CompleteUserSearchResult CreateCompleteUserSearchResult(User u)
    {
        return new CompleteUserSearchResult(
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
            u.Profile.MapToUserProfile(),
            u.UserSecurity.MapToUserSecurityDto(),
            u.UserUsage.MapToUserUsageDto()
        );
    }

    /// <summary>
    /// Creates a basic user search result with essential user information only.
    /// </summary>
    /// <param name="u">The user entity.</param>
    /// <returns>A UserSearchResult containing basic user data.</returns>
    private static UserSearchResult CreateBasicUserSearchResult(User u)
    {
        return new UserSearchResult(
            u.Id,
            u.Email,
            u.Username,
            u.SubscriptionPlanId,
            u.IsActive,
            u.Permissions
        );
    }

    #endregion
}
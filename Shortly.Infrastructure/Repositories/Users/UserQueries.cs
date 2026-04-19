using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Common;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Profile.DTOs;
using Shortly.Core.Security.DTOs;
using Shortly.Core.Users.Contracts;
using Shortly.Core.Users.DTOs.Search;
using Shortly.Core.Users.DTOs.Usage;
using Shortly.Core.Users.DTOs.User;
using Shortly.Core.Users.Mappers;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.Users;

public class UserQueries(SqlServerDbContext dbContext, ILogger<UserQueries> logger)
    : IUserQueries
{
    /// <inheritdoc />
    public async Task<ForceUpdateUserResponse> ForceUpdateUserAsync(Guid userId, ForceUpdateUserRequest request)
    {
        logger.LogInformation("Starting force update for user {UserId}", userId);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var user = await LoadUserWithRelatedEntitiesAsync(userId);

            ValidateUserAndRelatedEntitiesExist(user, userId);

            logger.LogDebug("Found user {Username} for force update", user?.Username);

            UpdateAllUserEntities(user, request);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("Successfully force updated user {UserId}", userId);

            return CreateForceUpdateResponse(user);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during force update for user {UserId}", userId);
            throw new DatabaseException("Error during force update", ex);
        }
    }


    /// <inheritdoc />
    public async Task<bool> HardDeleteUserAsync(Guid userId, bool deleteOwnedShortUrls = false,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Starting hard delete for user {UserId}", userId);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var user = await GetUserForDeletionAsync(userId, deleteOwnedShortUrls, cancellationToken);

            if (user is null)
            {
                logger.LogError("Hard delete failed: User with ID {UserId} not found.", userId);
                throw new NotFoundException("User", userId);
            }

            await DeleteUserShortUrlsIfRequiredAsync(user, userId);
            dbContext.Users.Remove(user);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogWarning("User {UserId} and associated data deleted successfully.", userId);
            return true;
        }
        catch (DbUpdateException ex) when (IsForeignKeyConstraintException(ex))
        {
            logger.LogError(ex,
                "Cannot delete user {UserId}: User owns organizations and cannot be deleted due to foreign key constraints",
                userId);
            throw new InvalidOperationException(
                $"Cannot delete user {userId}. The user owns organizations and must transfer ownership or delete organizations before user deletion.",
                ex);
        }
        catch (Exception ex) when (ex is not NotFoundException and not InvalidOperationException)
        {
            logger.LogError(ex, "Error during hard delete operation for user ID: {UserId}", userId);
            throw new DatabaseException("Failed to delete user", ex);
        }
    }


    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkActivateUsersAsync(ICollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var idsCount = userIds.Count();
        logger.LogInformation("Starting bulk activation for {Count} users", idsCount);

        try
        {
            var successCount = await dbContext.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsActive)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.IsActive, true)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken);

            var skippedCount = idsCount - successCount;
            logger.LogInformation(
                "Bulk activation completed. {SuccessCount} users activated, {SkippedCount} users were already active or don't exist.",
                successCount, skippedCount);

            return new BulkOperationResult(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during bulk user activation for {Count} users", idsCount);
            throw new DatabaseException("Bulk user activation failed.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkDeactivateUsersAsync(ICollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var idsCount = userIds.Count;
        logger.LogInformation("Starting bulk deactivation for {Count} users", idsCount);

        try
        {
            var successCount = await dbContext.Users
                .Where(u => userIds.Contains(u.Id) && u.IsActive)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.IsActive, false)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken);

            var skippedCount = idsCount - successCount;

            return new BulkOperationResult(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during bulk user deactivation for {Count} users", idsCount);
            throw new DatabaseException("Bulk user deactivation failed.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkDeleteUsersAsync(ICollection<Guid> userIds, Guid deletedBy,
        CancellationToken cancellationToken = default)
    {
        var idsCount = userIds.Count;
        logger.LogWarning("Starting bulk soft deletion for {Count} users by user {DeletedBy}", idsCount, deletedBy);

        try
        {
            var successCount = await dbContext.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.IsDeleted, true)
                        .SetProperty(u => u.IsActive, false) // Also deactivate during deletion
                        .SetProperty(u => u.DeletedAt, DateTime.UtcNow)
                        .SetProperty(u => u.DeletedBy, deletedBy)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken);

            var skippedCount = idsCount - successCount;

            return new BulkOperationResult(idsCount, successCount, skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during bulk soft deletion for {TotalCount} users.", idsCount);
            throw new DatabaseException("Bulk user soft deletion failed.", ex);
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
    
    


    #region Private Helper Methods

    /// <summary>
    ///     Loads the user with all required related entities for force update operation.
    /// </summary>
    /// <param name="userId">The user ID to load.</param>
    /// <returns>The user entity with all related entities loaded, or null if not found.</returns>
    private async Task<User?> LoadUserWithRelatedEntitiesAsync(Guid userId)
    {
        return await dbContext.Users
            .Include(u => u.Profile)
            .Include(u => u.UserSecurity)
            .Include(u => u.UserUsage)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>
    ///     Validates that the user and all required related entities exist.
    /// </summary>
    /// <param name="user">The user entity to validate.</param>
    /// <param name="userId">The user ID for error reporting.</param>
    /// <exception cref="NotFoundException">Thrown when user or any required entity is missing.</exception>
    private static void ValidateUserAndRelatedEntitiesExist(User? user, Guid userId)
    {
        if (user?.Profile is null || user.UserSecurity is null || user.UserUsage is null)
            throw new NotFoundException("User or required related entity", userId);
    }

    /// <summary>
    ///     Updates all user entities with the provided request data.
    /// </summary>
    /// <param name="user">The user entity to update.</param>
    /// <param name="request">The request containing update data for all entities.</param>
    private void UpdateAllUserEntities(User user, ForceUpdateUserRequest request)
    {
        UpdateUser(user, request.User);
        UpdateUserProfile(user.Profile, request.Profile);
        UpdateUserSecurity(user.UserSecurity, request.Security);
        UpdateUserUsage(user.UserUsage, request.Usage);
    }

    /// <summary>
    ///     Creates the response object with all updated user data.
    /// </summary>
    /// <param name="user">The updated user entity with related data.</param>
    /// <returns>The complete force update response.</returns>
    private static ForceUpdateUserResponse CreateForceUpdateResponse(User user)
    {
        return new ForceUpdateUserResponse(
            user.MapToUserDto(),
            user.Profile.MapToUserProfile(),
            user.UserSecurity.MapToUserSecurityDto(),
            user.UserUsage.MapToUserUsageDto()
        );
    }


    /// <summary>
    ///     Updates an existing user profile with new data.
    /// </summary>
    /// <param name="user">The existing user to update</param>
    /// <param name="dto">The user update data</param>
    private static void UpdateUser(User user, UserDto dto)
    {
        user.Email = dto.Email;
        user.Username = dto.Username;
        user.SubscriptionPlanId = dto.SubscriptionPlanId;
        user.Permissions = dto.Permissions;
        user.IsActive = dto.IsActive;
        user.IsEmailConfirmed = dto.IsEmailConfirmed;
        user.UpdatedAt = dto.UpdatedAt;
        user.CreatedAt = dto.CreatedAt;
        user.IsDeleted = dto.IsDeleted;
        user.DeletedAt = dto.DeletedAt;
        user.DeletedBy = dto.DeletedBy;
    }

    /// <summary>
    ///     Updates an existing user profile with new data.
    /// </summary>
    /// <param name="profile">The existing user profile to update</param>
    /// <param name="response">The profile update data</param>
    private static void UpdateUserProfile(UserProfile profile, UserProfileResponse response)
    {
        profile.Name = response.Name;
        profile.Bio = response.Bio;
        profile.PhoneNumber = response.PhoneNumber;
        profile.ProfilePictureUrl = response.ProfilePictureUrl;
        profile.Website = response.Website;
        profile.Company = response.Company;
        profile.Location = response.Location;
        profile.Country = response.Country;
        profile.TimeZone = response.TimeZone;
        profile.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Updates an existing user security record with new data.
    /// </summary>
    /// <param name="security">The existing user security record to update</param>
    /// <param name="dto">The security update data</param>
    private static void UpdateUserSecurity(UserSecurity security, UserSecurityDto dto)
    {
        security.FailedLoginAttempts = dto.FailedLoginAttempts;
        security.LockedUntil = dto.LockedUntil;
        security.TwoFactorEnabled = dto.TwoFactorEnabled;
        security.TwoFactorSecret = dto.TwoFactorSecret;
        security.UpdatedAt = dto.UpdatedAt;
    }

    /// <summary>
    ///     Updates an existing user usage record with new data.
    /// </summary>
    /// <param name="usage">The existing user usage record to update</param>
    /// <param name="dto">The usage update data</param>
    private static void UpdateUserUsage(UserUsage usage, UserUsageDto dto)
    {
        usage.MonthlyLinksCreated = dto.MonthlyLinksCreated;
        usage.MonthlyQrCodesCreated = dto.MonthlyQrCodesCreated;
        usage.TotalLinksCreated = dto.TotalLinksCreated;
        usage.TotalQrCodesCreated = dto.TotalQrCodesCreated;
        usage.MonthlyResetDate = dto.MonthlyResetDate;
    }


    /// <summary>
    ///     Retrieves the user for deletion, optionally including owned short URLs.
    /// </summary>
    /// <param name="userId">The user ID to retrieve.</param>
    /// <param name="includeShortUrls">Whether to include owned short URLs in the query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user entity or null if not found.</returns>
    private async Task<User?> GetUserForDeletionAsync(Guid userId, bool includeShortUrls,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsQueryable();

        if (includeShortUrls) query = query.Include(u => u.OwnedShortUrls);

        return await query.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <summary>
    ///     Deletes the user's short URLs if they exist and logs the operation.
    /// </summary>
    /// <param name="user">The user whose short URLs should be deleted.</param>
    /// <param name="userId">The user ID for logging purposes.</param>
    private Task DeleteUserShortUrlsIfRequiredAsync(User user, Guid userId)
    {
        if (user.OwnedShortUrls?.Count > 0)
        {
            dbContext.ShortUrls.RemoveRange(user.OwnedShortUrls);
            logger.LogInformation(
                "Deleted {Count} short URLs for user {UserId}.",
                user.OwnedShortUrls.Count,
                userId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Determines if the exception is caused by a foreign key constraint violation.
    /// </summary>
    /// <param name="ex">The database updates exception to check.</param>
    /// <returns>True if the exception is due to foreign key constraints, false otherwise.</returns>
    private static bool IsForeignKeyConstraintException(DbUpdateException ex)
    {
        // Check for common foreign key constraint error patterns
        var message = ex.InnerException?.Message?.ToLower() ?? string.Empty;

        return message.Contains("foreign key constraint") ||
               message.Contains("reference constraint") ||
               message.Contains("delete statement conflicted with the reference constraint") ||
               message.Contains("cannot delete or update a parent row");
    }

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
    private IQueryable<User> BuildUserQuery1409496384(UserSearchRequest request)
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
    
    #endregion
}
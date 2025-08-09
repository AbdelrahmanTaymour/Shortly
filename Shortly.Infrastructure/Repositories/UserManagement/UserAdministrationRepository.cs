using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs;
using Shortly.Core.DTOs.UsersDTOs.Profile;
using Shortly.Core.DTOs.UsersDTOs.Security;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UserManagement;

public class UserAdministrationRepository(SQLServerDbContext dbContext, ILogger<UserAdministrationRepository> logger)
    : IUserAdministrationRepository
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
        security.PasswordResetToken = dto.PasswordResetToken;
        security.TokenExpiresAt = dto.TokenExpiresAt;
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
    private async Task DeleteUserShortUrlsIfRequiredAsync(User user, Guid userId)
    {
        if (user.OwnedShortUrls?.Count > 0)
        {
            dbContext.ShortUrls.RemoveRange(user.OwnedShortUrls);
            logger.LogInformation(
                "Deleted {Count} short URLs for user {UserId}.",
                user.OwnedShortUrls.Count,
                userId);
        }
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

    #endregion
}
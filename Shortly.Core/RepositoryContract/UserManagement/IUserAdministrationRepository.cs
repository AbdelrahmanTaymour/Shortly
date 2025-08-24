using Shortly.Core.DTOs;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;

namespace Shortly.Core.RepositoryContract.UserManagement;

public interface IUserAdministrationRepository
{
    // SuperAdmin-specific user management

    /// <summary>
    /// Forcefully updates a user and all their related entities (profile, security, and usage) 
    /// bypassing normal validation rules. This is typically used for administrative operations.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="request">
    /// The request containing all the updated data for the user and their related entities.
    /// Must include User, Profile, Security, and Usage update data.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous update operation.
    /// The task result contains the complete updated user data including all related entities.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Thrown when the user with the specified ID is not found, or when any required 
    /// related entity (Profile, UserSecurity, UserUsage) is missing.
    /// </exception>
    /// <exception cref="DatabaseException">
    /// Thrown when a database error occurs during the update operation.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> This is a force update operation that bypasses normal business 
    /// validation rules and should only be used for administrative purposes or data correction.
    /// </para>
    /// <para>
    /// This method updates the user and ALL related entities in a single transaction:
    /// </para>
    /// <list type="bullet">
    /// <item><description>User entity (basic user information)</description></item>
    /// <item><description>User Profile (profile-specific data)</description></item>
    /// <item><description>User Security (security settings and credentials)</description></item>
    /// <item><description>User Usage (usage statistics and limits)</description></item>
    /// </list>
    /// <para>
    /// All related entities must exist for the operation to succeed. If any required entity 
    /// is missing, the operation will fail with a <see cref="NotFoundException"/>.
    /// </para>
    /// </remarks>
    Task<ForceUpdateUserResponse> ForceUpdateUserAsync(Guid userId, ForceUpdateUserRequest request);


    /// <summary>
    /// Permanently deletes a user and optionally their owned short URLs from the database.
    /// This operation cannot be undone.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <param name="deleteOwnedShortUrls">
    /// If true, also deletes all short URLs owned by the user. 
    /// If false, only the user record is deleted (short URLs remain orphaned).
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous delete operation. 
    /// The task result contains true if the deletion was successful.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when the user with the specified ID is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during the deletion process.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the user owns organizations and cannot be deleted due to foreign key constraints.</exception>
    /// <remarks>
    /// <para>
    /// Important Constraint: Users who own organizations cannot be deleted due to database foreign key constraints.
    /// The service layer should validate this condition before calling this method.
    /// </para>
    /// <para>
    /// If a user owns organizations, this method will fail with an <see cref="InvalidOperationException"/>.
    /// To delete such users, the organizations must first be transferred to another owner or deleted.
    /// </para>
    /// </remarks>
    Task<bool> HardDeleteUserAsync(Guid userId, bool deleteOwnedShortUrls,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Performs bulk activation of users by setting their IsActive property to true.
    /// Only affects users that are currently inactive and exist in the database.
    /// </summary>
    /// <param name="userIds">Collection of user GUIDs to activate. Cannot be null or empty.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation. Defaults to CancellationToken.None.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a BulkOperationResult
    /// with details about the operation including total count, success count, and skipped count.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when an unexpected database error occurs during the operation.</exception>
    /// <remarks>
    /// This method updates the IsActive property to true and sets UpdatedAt to the current UTC time.
    /// Users that are already active or don't exist in the database will be skipped and counted in the skipped result.
    /// The operation is performed as a bulk update for better performance.
    /// </remarks>
    Task<BulkOperationResult> BulkActivateUsersAsync(ICollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>
    /// Performs bulk deactivation of users by setting their IsActive property to false.
    /// Only affects users that are currently active and exist in the database.
    /// </summary>
    /// <param name="userIds">Collection of user GUIDs to deactivate. Cannot be null or empty.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a BulkOperationResult
    /// with details about the operation including total count, success count, and skipped count.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when an unexpected database error occurs during the operation.</exception>
    /// <remarks>
    /// This method updates the IsActive property to false and sets UpdatedAt to the current UTC time.
    /// Users that are already inactive or don't exist in the database will be skipped and counted in the skipped result.
    /// The operation is performed as a bulk update for better performance.
    /// </remarks>
    Task<BulkOperationResult> BulkDeactivateUsersAsync(ICollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>
    /// Performs bulk soft deletion of users by marking them as deleted and inactive.
    /// Only affects users that are not already deleted and exist in the database.
    /// </summary>
    /// <param name="userIds">Collection of user GUIDs to soft delete. Cannot be null or empty.</param>
    /// <param name="deletedBy">GUID of the user performing the deletion operation. Used for audit purposes.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation. Defaults to CancellationToken.None.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a BulkOperationResult
    /// with details about the operation including total count, success count, and skipped count.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when an unexpected database error occurs during the operation.</exception>
    /// <remarks>
    /// This method performs a soft delete by setting IsDeleted to true, IsActive to false,
    /// DeletedAt to the current UTC time, DeletedBy to the specified user GUID, and UpdatedAt to the current UTC time.
    /// Users that are already deleted or don't exist in the database will be skipped and counted in the skipped result.
    /// The operation is performed as a bulk update for better performance.
    /// This operation generates a warning-level log entry due to the nature of user deletion.
    /// </remarks>
    Task<BulkOperationResult> BulkDeleteUsersAsync(ICollection<Guid> userIds, Guid deletedBy,
        CancellationToken cancellationToken = default);
}
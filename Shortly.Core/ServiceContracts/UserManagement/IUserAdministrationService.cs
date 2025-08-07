using Shortly.Core.DTOs;
using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts.UserManagement;

/// <summary>
/// Service class that provides administrative operations for user management.
/// Acts as a business logic layer that orchestrates user administration operations 
/// through repository patterns while enforcing business rules and validation.
/// </summary>
public interface IUserAdministrationService
{
    /// <summary>
    /// Performs a forced update of user data, bypassing normal validation rules.
    /// This method is designed for administrative overrides of standard user update processes.
    /// The operation is performed within a database transaction to ensure data consistency.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update. Must be a valid GUID.</param>
    /// <param name="request">The update request containing new user data including profile, security, and usage information.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a ForceUpdateUserResponse
    /// with the updated user, profile, security, and usage information.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when the specified user or related entities are not found.</exception>
    /// <exception cref="DatabaseException">Thrown when an unexpected database error occurs during the operation.</exception>
    /// <remarks>
    /// This method delegates directly to the administrative repository without additional validation (depends on the fluent validation).
    /// It updates all user-related entities (User, UserProfile, UserSecurity, UserUsage) in a single transaction.
    /// The operation logs information about the update process for auditing purposes.
    /// </remarks>
    Task<ForceUpdateUserResponse> ForceUpdateUserAsync(Guid userId, ForceUpdateUserRequest request);

    /// <summary>
    /// Permanently deletes a user from the system with optional cleanup of associated data.
    /// This operation cannot be undone and will completely remove the user and optionally their short URLs.
    /// The method enforces business rules to prevent deletion of users with active organization ownership.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete. Must be a valid GUID.</param>
    /// <param name="deleteOwnedShortUrls">
    /// Whether to also delete short URLs owned by the user. If false, short URLs will remain orphaned.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation. Defaults to CancellationToken.None.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value
    /// indicating whether the deletion was successful (true) or not (false).
    /// </returns>
    /// <exception cref="BusinessRuleException">
    /// Thrown when the user owns any organization and cannot be deleted due to business rules.
    /// </exception>
    /// <exception cref="NotFoundException">Thrown when the specified user is not found in the database.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when foreign key constraints prevent deletion due to organizational ownership or other dependencies.
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when an unexpected database error occurs during the operation.</exception>
    /// <remarks>
    /// This method performs the following checks and operations:
    /// 1. Validates that the user is not an owner of any organization
    /// 2. Deletes the user's short URLs if specified
    /// 3. Permanently removes the user record from the database
    /// The operation is performed within a database transaction and generates warning-level logs due to its destructive nature.
    /// Foreign key constraint violations will result in detailed error messages suggesting remediation steps.
    /// </remarks>
    Task<bool> HardDeleteUserAsync(Guid userId, bool deleteOwnedShortUrls,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Performs bulk activation of users by setting their IsActive property to true.
    /// Only affects users that are currently inactive and exist in the database.
    /// This method includes validation of input parameters and delegates to the repository for the actual operation.
    /// </summary>
    /// <param name="userIds">Collection of user GUIDs to activate. Cannot be null or empty.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation. Defaults to CancellationToken.None.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a BulkOperationResult
    /// with details about the operation including total count, success count, and skipped count.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when userIds parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when userIds collection is empty.</exception>
    /// <exception cref="DatabaseException">Thrown when an unexpected database error occurs during the bulk operation.</exception>
    /// <remarks>
    /// This method performs input validation before delegating to the repository:
    /// - Ensures the userIds collection is not null
    /// - Ensures the userIds collection is not empty
    /// The actual bulk update operation sets IsActive to true and UpdatedAt to the current UTC time.
    /// Users that are already active or don't exist will be skipped and counted in the result.
    /// </remarks>
    Task<BulkOperationResult> BulkActivateUsersAsync(ICollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk deactivation of users by setting their IsActive property to false.
    /// Only affects users that are currently active and exist in the database.
    /// This method includes validation of input parameters and delegates to the repository for the actual operation.
    /// </summary>
    /// <param name="userIds">Collection of user GUIDs to deactivate. Cannot be null or empty.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation. Defaults to CancellationToken.None.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a BulkOperationResult
    /// with details about the operation including total count, success count, and skipped count.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when userIds parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when userIds collection is empty.</exception>
    /// <exception cref="DatabaseException">Thrown when an unexpected database error occurs during the bulk operation.</exception>
    /// <remarks>
    /// This method performs input validation before delegating to the repository:
    /// - Ensures the userIds collection is not null
    /// - Ensures the userIds collection is not empty
    /// The actual bulk update operation sets IsActive to false and UpdatedAt to the current UTC time.
    /// Users that are already inactive or don't exist will be skipped and counted in the result.
    /// </remarks>
    Task<BulkOperationResult> BulkDeactivateUsersAsync(ICollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk soft deletion of users by marking them as deleted and inactive with audit information.
    /// Only affects users that are not already deleted and exist in the database.
    /// This method includes comprehensive validation of input parameters and delegates to the repository for the actual operation.
    /// </summary>
    /// <param name="userIds">Collection of user GUIDs to soft delete. Cannot be null or empty.</param>
    /// <param name="deletedBy">GUID of the user performing the deletion operation. Used for audit purposes. Cannot be empty.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation. Defaults to CancellationToken.None.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a BulkOperationResult
    /// with details about the operation including total count, success count, and skipped count.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when userIds parameter is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when userIds collection is empty or when deletedBy is an empty GUID.
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when an unexpected database error occurs during the bulk operation.</exception>
    /// <remarks>
    /// This method performs comprehensive input validation before delegating to the repository:
    /// - Ensures the userIds collection is not null
    /// - Ensures the userIds collection is not empty  
    /// - Ensures the deletedBy GUID is not empty
    /// The actual bulk update operation performs soft deletion by setting:
    /// - IsDeleted to true
    /// - IsActive to false (users are deactivated during deletion)
    /// - DeletedAt to the current UTC time
    /// - DeletedBy to the specified user GUID for audit trails
    /// - UpdatedAt to the current UTC time
    /// Users that are already deleted or don't exist will be skipped and counted in the result.
    /// This operation generates warning-level log entries due to the nature of user deletion.
    /// </remarks>
    Task<BulkOperationResult> BulkDeleteUsersAsync(ICollection<Guid> userIds, Guid deletedBy,
        CancellationToken cancellationToken = default);
}
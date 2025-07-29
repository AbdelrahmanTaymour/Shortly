using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts;

// TODO: Update the Exception thrown in the documentation

/// <summary>
///     Provides high-level operations for user management in the application.
/// </summary>
public interface IUserService
{
    /// <summary>
    ///     Retrieves all users in the system.
    /// </summary>
    /// <returns>A collection of all users represented as <see cref="UserDto" /> objects.</returns>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    
    /// <summary>
    ///     Retrieves a specific user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <returns>The user details as <see cref="UserDto" /> if found.</returns>
    /// <exception cref="Exception">Thrown when the user is not found.</exception>
    Task<UserDto> GetUserByIdAsync(Guid id);
    
    /// <summary>
    ///     Creates a new user in the system.
    /// </summary>
    /// <param name="createUserRequest">The user creation details including required information.</param>
    /// <returns><see cref="CreateUserResponse"/> containing the created user information.</returns>
    /// <exception cref="Exception">Thrown when user creation fails or a user with same email/username exists.</exception>
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest createUserRequest);
    
    
    /// <summary>
    ///     Updates an existing user's information.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="updateUserDto">The updated user information.</param>
    /// <returns>The updated user details as <see cref="UserDto"/>.</returns>
    /// <exception cref="Exception">Thrown when the user is not found or update fails.</exception>
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
    
    
    /// <summary>
    ///     Permanently removes a user from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <returns>True if deletion was successful; otherwise throws an exception.</returns>
    /// <exception cref="Exception">Thrown when the user is not found.</exception>
    Task<bool> HardDeleteUserAsync(Guid id);
    
    
    /// <summary>
    ///     Marks a user as deleted without removing them from the database.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to soft delete.</param>
    /// <param name="deletedBy">The unique identifier of the admin performing the deletion.</param>
    /// <returns>True if the soft deletion was successful; otherwise false.</returns>
    Task<bool> SoftDeleteUserAccount(Guid userId, Guid deletedBy);
    
    
    /// <summary>
    ///     Locks a user account until a specified time.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to lock.</param>
    /// <param name="lockUntil">The datetime until which the user should be locked. Null for indefinite lock.</param>
    /// <returns>True if the lock was successful; otherwise throws an exception.</returns>
    /// <exception cref="Exception">Thrown when the user is not found.</exception>
    Task<bool> LockUser(Guid userId, DateTime? lockUntil);
    
    
    /// <summary>
    ///     Removes the lock from a user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to unlock.</param>
    /// <returns>True if the unlock was successful; otherwise throws an exception.</returns>
    /// <exception cref="Exception">Thrown when the user is not found.</exception>
    Task<bool> UnlockUser(Guid userId);
    

    /// <summary>
    ///     Activates a user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to activate.</param>
    /// <returns>True if activation was successful; otherwise throws an exception.</returns>
    /// <exception cref="Exception">Thrown when the user is not found.</exception>
    Task<bool> ActivateUser(Guid userId);
    
    
    /// <summary>
    ///     Deactivates a user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to deactivate.</param>
    /// <returns>True if deactivation was successful; otherwise throws an exception.</returns>
    /// <exception cref="Exception">Thrown when the user is not found.</exception>
    Task<bool> DeactivateUser(Guid userId);
    
    
    /// <summary>
    ///     Retrieves information about a user's availability status.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>User availability information if found; otherwise throws an exception.</returns>
    /// <exception cref="Exception">Thrown when the user is not found.</exception>
    Task<UserAvailabilityInfo?> GetUserAvailabilityInfo(Guid userId);
    
    
    /// <summary>
    ///     Searches for users based on various criteria with pagination.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter users.</param>
    /// <param name="role">Optional role to filter users.</param>
    /// <param name="subscriptionPlan">Optional subscription plan to filter users.</param>
    /// <param name="isActive">Optional active status to filter users.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns><see cref="UserSearchResponse"/> containg a paginated response containing matching users and total count information.</returns>
    Task<UserSearchResponse> SearchUsers(
        string? searchTerm,
        enUserRole? role,
        enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        int page,
        int pageSize);
}
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.RepositoryContract;

/// <summary>
///     Provides data access operations for user management.
/// </summary>
public interface IUserRepository
{
    #region Admin

    /// <summary>
    ///     Retrieves all users from the database.
    /// </summary>
    /// <returns>A collection of <see cref="User"/>.</returns>
    Task<IEnumerable<User>> GetAll();

    
    /// <summary>
    ///     Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetUserById(Guid userId);

    
    /// <summary>
    ///     Retrieves an active user by their email address.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <returns>The active user if found; otherwise null.</returns>
    Task<User?> GetActiveUserByEmail(string? email);

    
    /// <summary>
    ///     Retrieves an active user by their username.
    /// </summary>
    /// <param name="username">The username of the user.</param>
    /// <returns>The active user if found; otherwise null.</returns>
    Task<User?> GetActiveUserByUsername(string? username);

    
    /// <summary>
    ///     Retrieves an active user by their email and password combination.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="password">The password of the user.</param>
    /// <returns>The active user if found; otherwise null.</returns>
    Task<User?> GetActiveUserByEmailAndPassword(string? email, string? password);

    
    /// <summary>
    ///     Adds a new user to the database.
    /// </summary>
    /// <param name="user">The user entity to add.</param>
    /// <returns>The added user with generated identifier if successful; otherwise null.</returns>
    Task<User?> AddUser(User user);

    
    /// <summary>
    ///     Updates an existing user in the database.
    /// </summary>
    /// <param name="user">The user entity with updated information.</param>
    /// <returns>The updated user if successful; otherwise null.</returns>
    Task<User?> UpdateUser(User user);

    
    /// <summary>
    ///     Permanently removes a user from the database.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <returns>True if deletion was successful; otherwise false.</returns>
    Task<bool> HardDeleteUser(Guid userId);

    
    /// <summary>
    ///     Marks a user as deleted without removing them from the database.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to soft delete.</param>
    /// <param name="deletedBy">The unique identifier of the admin performing the deletion.</param>
    /// <returns>True if the soft deletion was successful; otherwise false.</returns>
    Task<bool> SoftDeleteUser(Guid userId, Guid deletedBy);

    
    /// <summary>
    ///     Locks a user account until a specified time.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to lock.</param>
    /// <param name="lockUntil">The datetime until which the user should be locked. Null for indefinite lock.</param>
    /// <returns>True if the lock was successful; otherwise false.</returns>
    Task<bool> LockUser(Guid userId, DateTime? lockUntil);

    
    /// <summary>
    ///     Removes the lock from a user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to unlock.</param>
    /// <returns>True if the unlock was successful; otherwise false.</returns>
    Task<bool> UnlockUser(Guid userId);

    
    /// <summary>
    ///     Activates a user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to activate.</param>
    /// <returns>True if activation was successful; otherwise false.</returns>
    Task<bool> ActivateUser(Guid userId);

    
    /// <summary>
    ///     Deactivates a user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to deactivate.</param>
    /// <returns>True if deactivation was successful; otherwise false.</returns>
    Task<bool> DeactivateUser(Guid userId);

    
    /// <summary>
    ///     Checks if a given email or username is already taken.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="username">The username to check.</param>
    /// <returns>True if either email or username is taken; otherwise false.</returns>
    Task<bool> IsEmailOrUsernameTaken(string email, string username);

    
    /// <summary>
    ///     Retrieves information about a user's availability status.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns><see cref="UserAvailabilityInfo"/> if found; otherwise null.</returns>
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
    /// <returns>A tuple containing the collection of <see cref="UserViewDto"/> and total count.</returns>
    Task<(IEnumerable<UserViewDto> Users, int TotalCount)> SearchUsers(
        string? searchTerm,
        enUserRole? role,
        enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        int page,
        int pageSize);

    #endregion
}
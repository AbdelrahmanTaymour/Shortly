using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UserManagement;

/// <summary>
/// Repository interface for managing user security data operations.
/// Provides methods for handling security-related information including login attempts and account locking.
/// </summary>
public interface IUserSecurityRepository
{
    /// <summary>
    /// Retrieves user security information by the associated user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose security information to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user security information if found; otherwise, null.</returns>
    Task<UserSecurity?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates user security information in the database.
    /// </summary>
    /// <param name="security">The user security entity with updated information.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateAsync(UserSecurity security, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Increments the failed login attempts counter for a user and updates the timestamp.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the increment was successful; otherwise, false.</returns>
    Task<bool> IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resets the failed login attempts counter to zero for a user and updates the timestamp.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the reset was successful; otherwise, false.</returns>
    Task<bool> ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Locks a user account until the specified date and time, and updates the timestamp.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to lock.</param>
    /// <param name="lockedUntil">The date and time until which the user should remain locked.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the lock was successful; otherwise, false.</returns>
    Task<bool> LockUserAsync(Guid userId, DateTime lockedUntil, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unlocks a user account by clearing the lock date, resetting failed login attempts, and updating the timestamp.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to unlock.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the unlock was successful; otherwise, false.</returns>
    Task<bool> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);

}
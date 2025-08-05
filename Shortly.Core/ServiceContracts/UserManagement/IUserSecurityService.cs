using Shortly.Core.Exceptions.ClientErrors;

namespace Shortly.Core.ServiceContracts.UserManagement;

/// <summary>
/// Defines operations related to user account security such as login failure tracking and account locking.
/// </summary>
public interface IUserSecurityService
{
    /// <summary>
    /// Records a failed login attempt for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the attempt was recorded successfully; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user's security record is not found.</exception>
    Task<bool> RecordFailedLoginAttemptAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resets the failed login attempt counter for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the reset was successful; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user's security record is not found.</exception>
    Task<bool> ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines whether the specified user account is currently locked.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the user is locked; otherwise, <c>false</c>.</returns>
    Task<bool> IsUserLockedAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Locks the specified user account until the given expiration date.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="lockUntil">The UTC date and time until which the user account should remain locked.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the lock was successful; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown if the user does not exist or is already locked.
    /// </exception>
    Task<bool> LockUserAsync(Guid userId, DateTime lockUntil, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unlocks the specified user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the user was successfully unlocked; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown if the user does not exist or is already unlocked.
    /// </exception>
    Task<bool> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
}
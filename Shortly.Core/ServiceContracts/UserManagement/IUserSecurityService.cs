using Shortly.Core.DTOs.UsersDTOs.Security;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;

namespace Shortly.Core.ServiceContracts.UserManagement;

/// <summary>
///     Defines operations related to user account security such as login failure tracking and account locking.
/// </summary>
public interface IUserSecurityService
{
    /// <summary>
    ///     Retrieves the security status of the specified user, including lock state,
    ///     failed login attempts, lock reason, and remaining days until unlock.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    ///     A <see cref="UserSecurityStatusResponse" /> containing information about the user's lock status,
    ///     failed login attempts, lock reason, and estimated days remaining until the account is unlocked.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown if the user's security record is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    ///     This method is typically used by administrators or internal systems to assess the current
    ///     security state of a user account. It determines whether the user is locked, how many failed
    ///     login attempts have occurred, the reason for the lock (if any), and how many days remain until
    ///     the lock expires (if applicable).
    /// </remarks>
    Task<UserSecurityStatusResponse> GetUserSecurityStatusAsync(Guid userId, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Records a failed login attempt for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the attempt was recorded successfully; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user's security record is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> RecordFailedLoginAttemptAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets the failed login attempt counter for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the reset was successful; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user's security record is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Determines whether the specified user account is currently locked.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the user is locked; otherwise, <c>false</c>.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> IsUserLockedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Locks the specified user account until the given expiration date.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="request">
    ///     <see cref="LockUserRequest" /> dto that contains
    ///     <c>LockUntil</c>: The UTC date and time until which the user account should remain locked.
    ///     <c>Reason</c>: The reason for locking the user
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Returns <see cref="LockUserResponse" /> confirmation that the user account was successfully locked.</returns>
    /// <exception cref="NotFoundException">
    ///     Thrown if the user does not exist or is already locked.
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<LockUserResponse> LockUserAsync(Guid userId, LockUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Unlocks the specified user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Returns <see cref="UnlockUserResponse" /> confirmation that the user account was successfully unlocked.</returns>
    /// <exception cref="NotFoundException">
    ///     Thrown if the user does not exist or is already unlocked.
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<UnlockUserResponse> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UserManagement;

/// <summary>
///     Repository interface for managing user security data operations.
///     Provides methods for handling security-related information including login attempts and account locking.
/// </summary>
public interface IUserSecurityRepository
{
    /// <summary>
    ///     Retrieves user security information by the associated user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose security information to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user security information if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses AsNoTracking for optimal read-only performance.</remarks>
    Task<UserSecurity?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates user security information in the database.
    /// </summary>
    /// <param name="security">The user security entity with updated information.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> UpdateAsync(UserSecurity security, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Increments the failed login attempts counter for a user and updates the timestamp.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the increment was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance atomic increment without loading entity into memory.</remarks>
    Task<bool> IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets the failed login attempts counter to zero for a user and updates the timestamp.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the reset was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance atomic reset without loading entity into memory.</remarks>
    Task<bool> ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether the specified user account is currently locked.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains <c>true</c> if the user is locked;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs while checking the user's lock status.</exception>
    Task<bool> IsUserLockedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Locks a user account until the specified date and time, and updates the timestamp.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to lock.</param>
    /// <param name="lockedUntil">The date and time until which the user should remain locked.</param>
    /// <param name="lockoutReason">The reason of locking the user</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the lock was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance lock operation without loading entity into memory.</remarks>
    Task<bool> LockUserAsync(Guid userId, DateTime lockedUntil, string? lockoutReason, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Unlocks a user account by clearing the lock date, resetting failed login attempts, and updating the timestamp.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to unlock.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the unlock was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    ///     Uses ExecuteUpdateAsync for high-performance unlock operation that clears the lock date and resets failed attempts.
    ///     Performs comprehensive unlock in a single atomic operation.
    /// </remarks>
    Task<bool> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves a paginated list of users who are currently locked based on the <c>LockedUntil</c> timestamp.
    /// </summary>
    /// <param name="page">The page number to retrieve (1-based index).</param>
    /// <param name="pageSize">The number of users to retrieve per page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a collection of locked
    ///     <see cref="User" /> entities.
    /// </returns>
    /// <exception cref="DatabaseException">
    ///     Thrown when an error occurs while querying the database for locked users.
    /// </exception>
    /// <remarks>
    ///     A user is considered locked if their <c>UserSecurity.LockedUntil</c> value is set and is later than the current UTC
    ///     time.
    /// </remarks>
    Task<IEnumerable<User>> GetLockedUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.UserManagement;

namespace Shortly.Core.Services.UserManagement;

/// <summary>
/// Provides security-related operations for user accounts, such as login tracking and account locking.
/// </summary>
public class UserSecurityService(IUserSecurityRepository securityRepository, ILogger<UserSecurityService> logger)
    : IUserSecurityService
{
    /// <inheritdoc/>
    public async Task<bool> RecordFailedLoginAttemptAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var recorded = await securityRepository.IncrementFailedLoginAttemptsAsync(userId, cancellationToken);
        if (!recorded)
            throw new NotFoundException("UserSecurity", userId);
        return recorded;
    }

    /// <inheritdoc/>
    public async Task<bool> ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var done = await securityRepository.ResetFailedLoginAttemptsAsync(userId, cancellationToken);
        if (!done)
            throw new NotFoundException("UserSecurity", userId);
        return done;
    }

    /// <inheritdoc/>
    public async Task<bool> IsUserLockedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await securityRepository.IsUserLockedAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> LockUserAsync(Guid userId, DateTime lockUntil,
        CancellationToken cancellationToken = default)
    {
        var locked = await securityRepository.LockUserAsync(userId, lockUntil, cancellationToken);
        if (!locked)
            throw new NotFoundException($"User with id {userId} not found or already locked.");
        return locked;
    }

    /// <inheritdoc/>
    public async Task<bool> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unLocked = await securityRepository.UnlockUserAsync(userId, cancellationToken);
        if (!unLocked)
            throw new NotFoundException($"User with id {userId} not found or already unlocked.");
        return unLocked;
    }
}
namespace Shortly.Core.ServiceContracts.UserManagement;

public interface IUserSecurityService
{
    // Account security
    Task<bool> ValidateUserAccessAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RecordFailedLoginAttemptAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserLockedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> LockUserAsync(Guid userId, DateTime? lockUntil, string reason, CancellationToken cancellationToken = default);
    Task<bool> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
}
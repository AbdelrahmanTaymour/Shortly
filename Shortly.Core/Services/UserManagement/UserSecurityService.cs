using Shortly.Core.ServiceContracts.UserManagement;

namespace Shortly.Core.Services.UserManagement;

public class UserSecurityService : IUserSecurityService
{
    public Task<bool> ValidateUserAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RecordFailedLoginAttemptAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsUserLockedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> LockUserAsync(Guid userId, DateTime? lockUntil, string reason, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
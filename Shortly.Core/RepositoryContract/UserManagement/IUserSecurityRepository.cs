using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UserManagement;

public interface IUserSecurityRepository
{
    Task<UserSecurity?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(UserSecurity security, CancellationToken cancellationToken = default);
    Task<bool> IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> LockUserAsync(Guid userId, DateTime lockedUntil, CancellationToken cancellationToken = default);
    Task<bool> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);

}
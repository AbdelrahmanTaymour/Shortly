using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UserManagement;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
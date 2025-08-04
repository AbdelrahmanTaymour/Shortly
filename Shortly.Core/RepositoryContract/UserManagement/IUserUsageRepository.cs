using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UserManagement;

public interface IUserUsageRepository
{
    Task<UserUsage?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(UserUsage usage, CancellationToken cancellationToken = default);
    Task<bool> IncrementLinksCreatedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IncrementQrCodesCreatedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ResetMonthlyUsageAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserUsage>> GetUsersForMonthlyResetAsync(DateTime date, CancellationToken cancellationToken = default);

}
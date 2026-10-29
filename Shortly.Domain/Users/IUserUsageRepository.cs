
namespace Shortly.Domain.Users;

public interface IUserUsageRepository
{
    Task<int> ResetAllMonthlyAsync(CancellationToken cancellationToken = default);
}
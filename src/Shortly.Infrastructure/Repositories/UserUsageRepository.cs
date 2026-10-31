using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Repositories;

public class UserUsageRepository(ApplicationDbContext dbContext)
    : IUserUsageRepository
{
    public async Task<int> ResetAllMonthlyAsync(CancellationToken cancellationToken = default)
        => await dbContext.Set<UserUsage>().ExecuteUpdateAsync(
            s => s.SetProperty(x => x.MonthlyLinksCreated, 0)
                .SetProperty(x => x.MonthlyQrCodesCreated, 0)
                .SetProperty(x => x.MonthlyResetDate, DateTime.UtcNow.AddMonths(1)),
            cancellationToken);
}
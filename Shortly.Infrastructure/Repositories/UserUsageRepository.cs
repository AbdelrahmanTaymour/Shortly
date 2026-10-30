using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Repositories;

/// <summary>
/// SQL Server implementation of the user usage repository.
/// </summary>
/// <remarks>
/// Uses bulk update operations (ExecuteUpdateAsync) for optimal performance when incrementing usage counters.
/// Designed for high-frequency operations like tracking link and QR code creation.
/// </remarks>
public class UserUsageRepository(ApplicationDbContext dbContext, ILogger<UserUsageRepository> logger)
    : IUserUsageRepository
{
    public async Task<int> ResetAllMonthlyAsync(CancellationToken cancellationToken = default)
        => await dbContext.Set<UserUsage>().ExecuteUpdateAsync(
            s => s.SetProperty(x => x.MonthlyLinksCreated, 0)
                .SetProperty(x => x.MonthlyQrCodesCreated, 0)
                .SetProperty(x => x.MonthlyResetDate, DateTime.UtcNow.AddMonths(1)),
            cancellationToken);
}
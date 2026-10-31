using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Organizations;

namespace Shortly.Infrastructure.Repositories;

internal sealed class OrganizationUsageRepository(ApplicationDbContext dbContext) : IOrganizationUsageRepository
{
    public async Task<bool> IncrementLinksCreatedAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await dbContext.Set<OrganizationUsage>()
            .Where(ou => ou.OrganizationId == organizationId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ou => ou.MonthlyLinksCreated, ou => ou.MonthlyLinksCreated + 1)
                .SetProperty(ou => ou.TotalLinksCreated, ou => ou.TotalLinksCreated + 1), cancellationToken) > 0;

    public async Task<bool> IncrementQrCodesCreatedAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await dbContext.Set<OrganizationUsage>()
            .Where(ou => ou.OrganizationId == organizationId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ou => ou.MonthlyQrCodesCreated, ou => ou.MonthlyQrCodesCreated + 1)
                .SetProperty(ou => ou.TotalQrCodesCreated, ou => ou.TotalQrCodesCreated + 1), cancellationToken) > 0;

    public async Task<int> ResetAllMonthlyAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        => await dbContext.Set<OrganizationUsage>()
            .Where(ou => ou.MonthlyResetDate < utcNow)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ou => ou.MonthlyLinksCreated, 0)
                .SetProperty(ou => ou.MonthlyQrCodesCreated, 0)
                .SetProperty(ou => ou.MonthlyResetDate, utcNow.AddMonths(1)), cancellationToken);
}
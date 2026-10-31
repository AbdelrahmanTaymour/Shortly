namespace Shortly.Domain.Organizations;

public interface IOrganizationUsageRepository
{
    Task<bool> IncrementLinksCreatedAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> IncrementQrCodesCreatedAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> ResetAllMonthlyAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
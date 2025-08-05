using Shortly.Core.DTOs.UsersDTOs.Usage;

namespace Shortly.Core.ServiceContracts.UserManagement;

public interface IUserUsageService
{
    Task<bool> TrackLinkCreationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> TrackQrCodeCreationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanCreateMoreLinksAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetRemainingLinksAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserUsageDto?> GetUsageStatsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasExceededLimitsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ResetMonthlyUsageAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ResetAllMonthlyUsageAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserUsageDto>> GetUsageReportAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
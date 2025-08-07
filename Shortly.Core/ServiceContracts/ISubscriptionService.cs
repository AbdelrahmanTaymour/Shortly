using Shortly.Core.DTOs.SubscriptionDTOs;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts;

public interface ISubscriptionService
{
    Task<SubscriptionInfoDto?> GetSubscriptionInfoAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> UpgradeSubscriptionAsync(Guid userId, enSubscriptionPlan plan,
        CancellationToken cancellationToken = default);

    Task<bool> DowngradeSubscriptionAsync(Guid userId, enSubscriptionPlan plan,
        CancellationToken cancellationToken = default);

    Task<bool> CancelSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsSubscriptionActiveAsync(Guid userId, CancellationToken cancellationToken = default);
}
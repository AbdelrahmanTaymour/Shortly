using Shortly.Domain.Enums;

namespace Shortly.Core.Abstractions.Cache;

public interface ISubscriptionPlanLimitsProvider
{
    Task<int> GetMaxLinksPerMonthAsync(enSubscriptionPlan planId, CancellationToken cancellationToken = default);
}
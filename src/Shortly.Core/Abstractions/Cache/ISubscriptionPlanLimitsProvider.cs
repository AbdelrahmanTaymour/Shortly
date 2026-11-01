using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Core.Abstractions.Cache;

public interface ISubscriptionPlanLimitsProvider
{
    Task<SubscriptionPlanLimits> GetLimitsAsync(enSubscriptionPlan planId, CancellationToken cancellationToken = default);
}

public sealed record SubscriptionPlanLimits(
    int MaxLinksPerMonth, int MaxQrCodesPerMonth, int ClickDataRetentionDays,
    bool LinkAnalysis, bool BulkCreation, bool LinkProtection,
    bool CustomShortCode, bool CampaignTracking, bool GeoDeviceTracking)
{
    public bool HasUnlimitedLinks => MaxLinksPerMonth < 0;
    public bool HasUnlimitedQrCodes => MaxQrCodesPerMonth < 0;
    public bool HasUnlimitedRetention => ClickDataRetentionDays < 0;
}
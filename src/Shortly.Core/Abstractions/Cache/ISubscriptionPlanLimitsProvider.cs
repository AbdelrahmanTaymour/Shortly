using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Core.Abstractions.Cache;

public interface ISubscriptionPlanLimitsProvider
{
    Task<SubscriptionPlanLimits> GetLimitsAsync(enSubscriptionPlan planId, CancellationToken cancellationToken = default);
}

public sealed record SubscriptionPlanLimits(
    int MaxLinksPerMonth, int MaxQrCodesPerMonth, int ClickDataRetentionDays,
    int MaxMembersPerOrganization, int MaxOrganizations,
    bool LinkAnalysis, bool BulkCreation, bool LinkProtection,
    bool CustomShortCode, bool CampaignTracking, bool GeoDeviceTracking)
{
    public bool HasUnlimitedLinks => MaxLinksPerMonth < 0;
    public bool HasUnlimitedQrCodes => MaxQrCodesPerMonth < 0;
    public bool HasUnlimitedRetention => ClickDataRetentionDays < 0;
    public bool HasUnlimitedMembers => MaxMembersPerOrganization < 0;
    public bool HasUnlimitedOrganizations => MaxOrganizations < 0;

    /// <summary>The member limit stored on an organization: the plan's cap, or "no practical limit" for an unlimited plan.</summary>
    public int OrganizationMemberLimit => HasUnlimitedMembers ? int.MaxValue : MaxMembersPerOrganization;
}
using Shortly.Domain.Abstractions;
using Shortly.Domain.Enums;

namespace Shortly.Domain.SubscriptionPlan;

public sealed class SubscriptionPlan : Entity<enSubscriptionPlan>
{
    public SubscriptionPlan(
        enSubscriptionPlan id, string name, string description, decimal price,
        int maxLinksPerMonth, int maxQrCodesPerMonth, int clickDataRetentionDays,
        bool linkAnalysis, bool bulkCreation, bool linkProtection,
        bool customShortCode, bool campaignTracking, bool geoDeviceTracking) : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        MaxLinksPerMonth = maxLinksPerMonth;
        MaxQrCodesPerMonth = maxQrCodesPerMonth;
        ClickDataRetentionDays = clickDataRetentionDays;
        LinkAnalysis = linkAnalysis;
        BulkCreation = bulkCreation;
        LinkProtection = linkProtection;
        CustomShortCode = customShortCode;
        CampaignTracking = campaignTracking;
        GeoDeviceTracking = geoDeviceTracking;
    }

    private SubscriptionPlan() { } // EF

    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int MaxLinksPerMonth { get; private set; }
    public int MaxQrCodesPerMonth { get; private set; }
    public int ClickDataRetentionDays { get; private set; }
    public bool LinkAnalysis { get; private set; }
    public bool BulkCreation { get; private set; }
    public bool LinkProtection { get; private set; }
    public bool CustomShortCode { get; private set; }
    public bool CampaignTracking { get; private set; }
    public bool GeoDeviceTracking { get; private set; }

    public bool HasUnlimitedLinks => MaxLinksPerMonth < 0;
    public bool HasUnlimitedQrCodes => MaxQrCodesPerMonth < 0;
    public bool HasUnlimitedRetention => ClickDataRetentionDays < 0;
}
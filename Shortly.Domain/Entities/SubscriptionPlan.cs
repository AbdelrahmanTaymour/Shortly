using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class SubscriptionPlan
{
    public enSubscriptionPlan Id { get; set; } // Same as enSubscriptionPlan enum value
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public int MaxQrCodesPerMonth { get; set; }
    public int MaxLinksPerMonth { get; set; }
    public int ClickDataRetentionDays { get; set; } // In days
    public bool LinkAnalysis { get; set; }
    public bool BulkCreation { get; set; }
    public bool LinkProtection { get; set; }
    public bool CustomShortCode { get; set; }
    public bool CampaignTracking { get; set; }
    public bool GeoDeviceTracking { get; set; }
}
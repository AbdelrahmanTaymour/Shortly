using Shortly.Domain.Abstractions;

namespace Shortly.Domain.SubscriptionPlan;

public sealed class SubscriptionPlan : Entity<enSubscriptionPlan>
{
    public SubscriptionPlan(
        enSubscriptionPlan id, string name, string description, decimal price,
        int maxLinksPerMonth, int maxQrCodesPerMonth, int clickDataRetentionDays,
        int maxMembersPerOrganization, int maxOrganizations,
        bool linkAnalysis, bool bulkCreation, bool linkProtection,
        bool customShortCode, bool campaignTracking, bool geoDeviceTracking) : base(id)
    {
        // Plans are configuration (seeded at model creation), so an invalid plan is a programming error, not user input.
        if (!Enum.IsDefined(id)) throw new ArgumentOutOfRangeException(nameof(id), id, "The plan id is not defined.");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ThrowIfInvalidLimit(maxLinksPerMonth, nameof(maxLinksPerMonth));
        ThrowIfInvalidLimit(maxQrCodesPerMonth, nameof(maxQrCodesPerMonth));
        ThrowIfInvalidLimit(clickDataRetentionDays, nameof(clickDataRetentionDays));
        ThrowIfInvalidCap(maxMembersPerOrganization, nameof(maxMembersPerOrganization));
        ThrowIfInvalidCap(maxOrganizations, nameof(maxOrganizations));

        Name = name;
        Description = description;
        Price = price;
        MaxLinksPerMonth = maxLinksPerMonth;
        MaxQrCodesPerMonth = maxQrCodesPerMonth;
        ClickDataRetentionDays = clickDataRetentionDays;
        MaxMembersPerOrganization = maxMembersPerOrganization;
        MaxOrganizations = maxOrganizations;
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
    public int MaxMembersPerOrganization { get; private set; }
    public int MaxOrganizations { get; private set; }
    public bool LinkAnalysis { get; private set; }
    public bool BulkCreation { get; private set; }
    public bool LinkProtection { get; private set; }
    public bool CustomShortCode { get; private set; }
    public bool CampaignTracking { get; private set; }
    public bool GeoDeviceTracking { get; private set; }

    public bool HasUnlimitedLinks => MaxLinksPerMonth < 0;
    public bool HasUnlimitedQrCodes => MaxQrCodesPerMonth < 0;
    public bool HasUnlimitedRetention => ClickDataRetentionDays < 0;
    public bool HasUnlimitedMembers => MaxMembersPerOrganization < 0;
    public bool HasUnlimitedOrganizations => MaxOrganizations < 0;

    /// <summary>A cap on members or organizations is either -1 (unlimited) or at least 1.</summary>
    private static void ThrowIfInvalidCap(int value, string parameterName)
    {
        if (value is not -1 and < 1)
            throw new ArgumentOutOfRangeException(parameterName, value, "A cap must be -1 (unlimited) or at least 1.");
    }

    /// <summary>A limit is either -1 (unlimited) or zero / a positive amount.</summary>
    private static void ThrowIfInvalidLimit(int value, string parameterName)
    {
        if (value < -1)
            throw new ArgumentOutOfRangeException(parameterName, value, "A limit must be -1 (unlimited), zero or positive.");
    }
}

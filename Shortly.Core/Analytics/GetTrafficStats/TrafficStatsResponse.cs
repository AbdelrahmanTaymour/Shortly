using Shortly.Core.Analytics.GetDeviceStats;

namespace Shortly.Core.Analytics.GetTrafficStats;

public sealed record ReferrerBreakdown(string Domain, int Clicks, double Percentage);
public sealed record CampaignBreakdown(string CampaignName, string? Source, string? Medium, int Clicks, double Percentage);

public sealed class TrafficStatsResponse
{
    public IReadOnlyList<DeviceBreakdown> TopTrafficSources { get; init; } = [];
    public IReadOnlyList<ReferrerBreakdown> TopReferrers { get; init; } = [];
    public IReadOnlyList<CampaignBreakdown> TopCampaigns { get; init; } = [];
    public double DirectPercentage { get; init; }
    public double SearchPercentage { get; init; }
    public double SocialPercentage { get; init; }
    public double ReferralPercentage { get; init; }
}
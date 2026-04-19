namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Traffic sources (top 6), referrer domains (top 10), UTM campaigns (top 10).
///     TOP N pushed into SQL.
///     <para>Endpoints:</para>
///     <para>  GET /api/statistics/urls/{id}/traffic</para>
///     <para>  GET /api/statistics/my-stats/traffic</para>
/// </summary>
public class TrafficStats
{
    public List<TrafficSourceBreakdown> TopTrafficSources { get; set; } = [];
    public List<ReferrerBreakdown> TopReferrers { get; set; } = [];
    public List<CampaignPerformance> TopCampaigns { get; set; } = [];
    public double DirectTrafficPercentage { get; set; }
    public double SearchTrafficPercentage { get; set; }
    public double SocialTrafficPercentage { get; set; }
    public double ReferralTrafficPercentage { get; set; }
}
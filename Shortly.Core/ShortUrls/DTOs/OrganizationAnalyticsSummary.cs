namespace Shortly.Core.ShortUrls.DTOs;

/// <summary>
/// Analytics summary for organization's short URL usage and team metrics.
/// </summary>
/// <remarks>
/// Provides organization-level insights including team collaboration metrics
/// and overall usage statistics for management reporting.
/// </remarks>
public record OrganizationAnalyticsSummary
{
    public int TotalUrls { get; set; }
    public int ActiveUrls { get; set; }
    public long TotalClicks { get; set; }
    public int MemberCount { get; set; }
    public double AverageClicksPerUrl { get; set; }
    public int InactiveUrls => TotalUrls - ActiveUrls;
    public double AverageUrlsPerMember => MemberCount > 0 ? (double)TotalUrls / MemberCount : 0.0;
}
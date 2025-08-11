namespace Shortly.Core.DTOs.ShortUrlDTOs;

/// <summary>
/// Analytics summary for organization's short URL usage and team metrics.
/// </summary>
/// <remarks>
/// Provides organization-level insights including team collaboration metrics
/// and overall usage statistics for management reporting.
/// </remarks>
public record OrganizationAnalyticsSummary
{
    /// <summary>
    /// Gets or sets the total number of short URLs in the organization.
    /// </summary>
    public int TotalUrls { get; set; }

    /// <summary>
    /// Gets or sets the number of currently active URLs.
    /// </summary>
    public int ActiveUrls { get; set; }

    /// <summary>
    /// Gets or sets the total number of clicks across all organization URLs.
    /// </summary>
    public long TotalClicks { get; set; }

    /// <summary>
    /// Gets or sets the number of unique members who have created URLs.
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Gets or sets the average clicks per URL across the organization.
    /// </summary>
    public double AverageClicksPerUrl { get; set; }

    /// <summary>
    /// Gets the number of inactive URLs.
    /// </summary>
    public int InactiveUrls => TotalUrls - ActiveUrls;

    /// <summary>
    /// Gets the average URLs created per active member.
    /// </summary>
    public double AverageUrlsPerMember => MemberCount > 0 ? (double)TotalUrls / MemberCount : 0.0;
}
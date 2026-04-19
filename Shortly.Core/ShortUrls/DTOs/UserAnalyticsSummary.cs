namespace Shortly.Core.ShortUrls.DTOs;

/// <summary>
/// Analytics summary for individual user's short URL usage.
/// </summary>
/// <remarks>
/// Provides comprehensive metrics for user dashboard and reporting features.
/// All counts are calculated based on the user's owned URLs only.
/// </remarks>
public record UserAnalyticsSummary
{
    public int TotalUrls { get; set; }
    public int ActiveUrls { get; set; }
    public long TotalClicks { get; set; }
    public int PrivateUrls { get; set; }
    public int PasswordProtectedUrls { get; set; }
    public int ExpiredUrls { get; set; }
    public int InactiveUrls => TotalUrls - ActiveUrls;
    public double AverageClicksPerUrl => TotalUrls > 0 ? (double)TotalClicks / TotalUrls : 0.0;
    public double ActiveUrlPercentage => TotalUrls > 0 ? (double)ActiveUrls / TotalUrls * 100 : 0.0;
}
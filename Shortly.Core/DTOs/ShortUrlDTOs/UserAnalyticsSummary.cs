namespace Shortly.Core.DTOs.ShortUrlDTOs;

/// <summary>
/// Analytics summary for individual user's short URL usage.
/// </summary>
/// <remarks>
/// Provides comprehensive metrics for user dashboard and reporting features.
/// All counts are calculated based on the user's owned URLs only.
/// </remarks>
public record UserAnalyticsSummary
{
    /// <summary>
    /// Gets or sets the total number of short URLs created by the user.
    /// </summary>
    public int TotalUrls { get; set; }

    /// <summary>
    /// Gets or sets the number of currently active URLs.
    /// </summary>
    public int ActiveUrls { get; set; }

    /// <summary>
    /// Gets or sets the total number of clicks across all user's URLs.
    /// </summary>
    public long TotalClicks { get; set; }

    /// <summary>
    /// Gets or sets the number of private URLs.
    /// </summary>
    public int PrivateUrls { get; set; }

    /// <summary>
    /// Gets or sets the number of password-protected URLs.
    /// </summary>
    public int PasswordProtectedUrls { get; set; }

    /// <summary>
    /// Gets or sets the number of expired URLs.
    /// </summary>
    public int ExpiredUrls { get; set; }

    /// <summary>
    /// Gets the number of inactive URLs.
    /// </summary>
    public int InactiveUrls => TotalUrls - ActiveUrls;

    /// <summary>
    /// Gets the average clicks per URL.
    /// </summary>
    public double AverageClicksPerUrl => TotalUrls > 0 ? (double)TotalClicks / TotalUrls : 0.0;

    /// <summary>
    /// Gets the percentage of URLs that are active.
    /// </summary>
    public double ActiveUrlPercentage => TotalUrls > 0 ? (double)ActiveUrls / TotalUrls * 100 : 0.0;
}
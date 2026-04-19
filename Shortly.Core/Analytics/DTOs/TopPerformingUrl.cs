namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Top performing URL for a user
/// </summary>
public class TopPerformingUrl
{
    public long ShortUrlId { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public long TotalClicks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastClickAt { get; set; }
    public string? TopCountry { get; set; }
}
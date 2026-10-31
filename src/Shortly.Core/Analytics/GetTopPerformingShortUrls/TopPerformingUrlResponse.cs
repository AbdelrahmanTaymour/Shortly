namespace Shortly.Core.Analytics.GetTopPerformingShortUrls;

public sealed class TopPerformingUrlResponse
{
    public long ShortUrlId { get; init; }
    public string ShortCode { get; init; } = string.Empty;
    public string OriginalUrl { get; init; } = string.Empty;
    public long TotalClicks { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastClickAtUtc { get; init; }
    public string? TopCountry { get; init; }
}
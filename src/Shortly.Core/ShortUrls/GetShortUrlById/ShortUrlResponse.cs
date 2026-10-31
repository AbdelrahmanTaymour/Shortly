namespace Shortly.Core.ShortUrls.GetShortUrlById;

public sealed class ShortUrlResponse
{
    public long Id { get; init; }
    public string OriginalUrl { get; init; } = string.Empty;
    public string ShortCode { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int TotalClicks { get; init; }
    public int ClickLimit { get; init; }
    public bool IsPrivate { get; init; }
    public bool IsPasswordProtected { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
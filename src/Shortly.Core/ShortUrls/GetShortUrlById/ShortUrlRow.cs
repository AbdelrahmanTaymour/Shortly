namespace Shortly.Core.ShortUrls.GetShortUrlById;

internal sealed class ShortUrlRow
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
    public int TotalCount { get; init; }

    public ShortUrlResponse ToResponse() => new()
    {
        Id = Id,
        OriginalUrl = OriginalUrl,
        ShortCode = ShortCode,
        IsActive = IsActive,
        TotalClicks = TotalClicks,
        ClickLimit = ClickLimit,
        IsPrivate = IsPrivate,
        IsPasswordProtected = IsPasswordProtected,
        ExpiresAt = ExpiresAt,
        Title = Title,
        Description = Description,
        CreatedAtUtc = CreatedAtUtc
    };
}
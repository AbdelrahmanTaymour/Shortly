namespace Shortly.API.Controllers.ShortUrls;

public sealed record UpdateShortUrlRequest(
    string OriginalUrl, string ShortCode, string? Title, string? Description,
    int ClickLimit, bool IsActive, bool IsPrivate, bool TrackingEnabled, DateTime? ExpiresAt);


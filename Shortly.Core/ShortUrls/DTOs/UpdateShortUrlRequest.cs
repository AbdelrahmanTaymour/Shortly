namespace Shortly.Core.ShortUrls.DTOs;

public record UpdateShortUrlRequest(
    string? OriginalUrl,
    bool? IsActive,
    bool? TrackingEnabled,
    int? ClickLimit,
    bool? IsPasswordProtected,
    string? Password,
    bool? IsPrivate,
    DateTime? ExpiresAt,
    string? Title,
    string? Description
);
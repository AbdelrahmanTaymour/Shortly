using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.UpdateShortUrl;

public sealed record UpdateShortUrlCommand(
    long ShortUrlId,
    string OriginalUrl,
    string ShortCode,
    string? Title,
    string? Description,
    int ClickLimit,
    bool IsActive,
    bool IsPrivate,
    bool TrackingEnabled,
    DateTime? ExpiresAt) : ICommand;
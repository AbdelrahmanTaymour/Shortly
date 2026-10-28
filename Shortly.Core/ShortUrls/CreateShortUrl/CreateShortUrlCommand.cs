using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.CreateShortUrl;

public sealed record CreateShortUrlCommand(
    string OriginalUrl,
    string? CustomShortCode,
    int ClickLimit,
    bool TrackingEnabled,
    bool IsPrivate,
    DateTime? ExpiresAt,
    string? Title,
    string? Description) : ICommand<long>;
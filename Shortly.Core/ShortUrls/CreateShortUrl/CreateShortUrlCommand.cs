using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.CreateShortUrl;

public sealed record CreateShortUrlCommand(
    string OriginalUrl,
    string? CustomShortCode,
    int? ClickLimit,
    bool? TrackingEnabled,
    bool? IsPrivate,
    DateTime? ExpiresAt,
    string? Title,
    string? Description) : ICommand<string>;
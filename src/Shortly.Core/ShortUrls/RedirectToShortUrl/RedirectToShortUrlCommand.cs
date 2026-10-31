using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.RedirectToShortUrl;

public sealed record RedirectToShortUrlCommand(
    string ShortCode,
    string? Password,
    string IpAddress,
    string SessionId,
    string UserAgent,
    string? Referrer,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    string? UtmTerm,
    string? UtmContent) : ICommand<string>;
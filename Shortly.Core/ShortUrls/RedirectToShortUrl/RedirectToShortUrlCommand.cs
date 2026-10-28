using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.RedirectToShortUrl;

public sealed record RedirectToShortUrlCommand(string ShortCode, string? Password) : ICommand<string>;

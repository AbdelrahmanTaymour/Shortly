using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.ActivateShortUrl;

public sealed record ActivateShortUrlCommand(long ShortUrlId) : ICommand;
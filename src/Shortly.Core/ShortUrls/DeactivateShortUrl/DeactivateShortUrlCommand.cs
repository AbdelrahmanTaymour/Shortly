using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.DeactivateShortUrl;

public sealed record DeactivateShortUrlCommand(long ShortUrlId) : ICommand;

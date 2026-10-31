using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.DeleteShortUrl;

public sealed record DeleteShortUrlCommand(long ShortUrlId) : ICommand;
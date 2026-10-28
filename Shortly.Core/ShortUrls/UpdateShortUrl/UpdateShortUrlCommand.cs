using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.UpdateShortUrl;

public sealed record UpdateShortUrlCommand(long ShortUrlId, string OriginalUrl, string? Title, string? Description) : ICommand;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.AssignShortCode;

public sealed record AssignShortCodeCommand(long ShortUrlId, string NewShortCode) : ICommand;

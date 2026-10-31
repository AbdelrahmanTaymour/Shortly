using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.RemoveShortUrlPassword;

public sealed record RemoveShortUrlPasswordCommand(long ShortUrlId) : ICommand;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.SetShortUrlPassword;

public sealed record SetShortUrlPasswordCommand(long ShortUrlId, string Password) : ICommand;
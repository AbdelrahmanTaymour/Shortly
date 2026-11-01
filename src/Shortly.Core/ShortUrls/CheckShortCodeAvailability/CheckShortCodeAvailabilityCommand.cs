using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.CheckShortCodeAvailability;

public sealed record CheckShortCodeAvailabilityCommand(string ShortCode) : ICommand<bool>;
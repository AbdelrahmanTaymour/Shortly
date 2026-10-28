using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.CheckShortCodeAvailability;

public sealed record CheckShortCodeAvailabilityQuery(string ShortCode) : IQuery<bool>;

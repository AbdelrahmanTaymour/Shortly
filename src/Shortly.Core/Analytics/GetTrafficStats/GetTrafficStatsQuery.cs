using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetTrafficStats;

public sealed record GetTrafficStatsQuery(
    long? ShortUrlId, DateTime? From, DateTime? To) : IQuery<TrafficStatsResponse>;


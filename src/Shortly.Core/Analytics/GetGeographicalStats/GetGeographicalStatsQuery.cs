using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetGeographicalStats;

public sealed record GetGeographicalStatsQuery(
    long? ShortUrlId, DateTime? From, DateTime? To) : IQuery<GeographicalStatsResponse>;
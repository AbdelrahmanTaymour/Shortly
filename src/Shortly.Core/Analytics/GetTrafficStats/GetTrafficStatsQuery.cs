using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetTrafficStats;

public sealed record GetTrafficStatsQuery(
    long? ShortUrlId, Guid? UserId, Guid? OrganizationId, DateTime? From, DateTime? To) : IQuery<TrafficStatsResponse>;


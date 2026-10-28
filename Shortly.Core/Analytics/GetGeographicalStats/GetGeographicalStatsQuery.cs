using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetGeographicalStats;

public sealed record GetGeographicalStatsQuery(
    long? ShortUrlId, Guid? UserId, Guid? OrganizationId, DateTime? From, DateTime? To) : IQuery<GeographicalStatsResponse>;
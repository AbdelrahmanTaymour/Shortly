using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetTimeSeriesStats;

public sealed record GetTimeSeriesStatsQuery(
    long? ShortUrlId, Guid? UserId, Guid? OrganizationId, DateTime From, DateTime To) : IQuery<TimeSeriesStatsResponse>;
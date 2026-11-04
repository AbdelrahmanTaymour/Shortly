using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetEngagementMetrics;

public sealed record GetEngagementMetricsQuery(
    long? ShortUrlId, DateTime? From, DateTime? To) : IQuery<EngagementMetricsResponse>;
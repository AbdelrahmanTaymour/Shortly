using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetAnalyticsOverview;

public sealed record GetAnalyticsOverviewQuery(
    long? ShortUrlId, DateTime? From, DateTime? To) : IQuery<AnalyticsOverviewResponse>;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetAnalyticsOverview;

public sealed record GetAnalyticsOverviewQuery(
    long? ShortUrlId, Guid? UserId, Guid? OrganizationId, DateTime? From, DateTime? To) : IQuery<AnalyticsOverviewResponse>;
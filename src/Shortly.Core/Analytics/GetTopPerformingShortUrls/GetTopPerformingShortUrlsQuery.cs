using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetTopPerformingShortUrls;

public sealed record GetTopPerformingShortUrlsQuery(int TopCount, TimeSpan? Timeframe)
    : IQuery<IReadOnlyList<TopPerformingUrlResponse>>;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetUrlsApproachingClickLimit;

public sealed record GetUrlsApproachingClickLimitQuery(double WarningThreshold, int Page, int PageSize)
    : IQuery<PagedResponse<ShortUrlResponse>>;
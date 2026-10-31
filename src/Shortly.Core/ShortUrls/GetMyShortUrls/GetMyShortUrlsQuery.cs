using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.ShortUrls.GetMyShortUrls;

public sealed record GetMyShortUrlsQuery(int Page, int PageSize) : IQuery<PagedResponse<ShortUrlResponse>>;
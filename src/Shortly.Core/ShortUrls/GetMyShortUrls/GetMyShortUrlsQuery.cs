using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.ShortUrls.GetMyShortUrls;

public sealed record GetMyShortUrlsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResponse<ShortUrlResponse>>;
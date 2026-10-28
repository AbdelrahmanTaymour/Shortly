using Shortly.Core.ShortUrls.SearchShortUrls;

namespace Shortly.API.Controllers.ShortUrls;

public sealed record SearchShortUrlsRequest(
    string? Search, ShortUrlStatusFilter Status, ShortUrlVisibilityFilter Visibility,
    DateTime? DateFrom, DateTime? DateTo, ShortUrlSort SortBy, int Page = 1, int PageSize = 10);
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.ShortUrls.SearchShortUrls;

public enum ShortUrlStatusFilter { All, Active, Inactive }
public enum ShortUrlVisibilityFilter { All, Public, Private }
public enum ShortUrlSort { Newest, Oldest, MostClicked }

public sealed record SearchShortUrlsQuery(
    string? Search,
    ShortUrlStatusFilter Status,
    ShortUrlVisibilityFilter Visibility,
    DateTime? DateFrom,
    DateTime? DateTo,
    ShortUrlSort SortBy,
    int Page,
    int PageSize) : IQuery<PagedResponse<ShortUrlResponse>>;
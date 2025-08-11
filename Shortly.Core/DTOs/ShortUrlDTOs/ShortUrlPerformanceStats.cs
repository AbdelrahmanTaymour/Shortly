namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record ShortUrlPerformanceStats
(
    long ShortUrlId,
    string ShortCode,
    int TotalClicks,
    int UniqueVisitors,
    DateTime LastClickedAt,
    decimal ClickThroughRate,
    IReadOnlyList<CountryClickStat> TopCountries,
    IReadOnlyList<ReferrerClickStat> TopReferrers
);
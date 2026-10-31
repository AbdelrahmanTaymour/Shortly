namespace Shortly.Core.Analytics.GetShortUrlPortfolioSummary;

public sealed class ShortUrlPortfolioSummaryResponse
{
    public int TotalUrls { get; init; }
    public int ActiveUrls { get; init; }
    public int ExpiredUrls { get; init; }
    public int PrivateUrls { get; init; }
    public int PasswordProtectedUrls { get; init; }
    public long TotalClicks { get; init; }
    public double AverageClicksPerUrl { get; init; }
}
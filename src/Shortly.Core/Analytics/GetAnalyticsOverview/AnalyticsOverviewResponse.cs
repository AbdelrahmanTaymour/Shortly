namespace Shortly.Core.Analytics.GetAnalyticsOverview;

public sealed class AnalyticsOverviewResponse
{
    public long TotalClicks { get; init; }
    public long UniqueClicks { get; init; }
    public int ActiveDays { get; init; }
    public double AverageClicksPerDay { get; init; }
    public DateTime? FirstClickAtUtc { get; init; }
    public DateTime? LastClickAtUtc { get; init; }
    public long ClicksToday { get; init; }
    public long ClicksThisWeek { get; init; }
    public long ClicksThisMonth { get; init; }
    public string? TopCountry { get; init; }
    public string? TopReferrer { get; init; }
}
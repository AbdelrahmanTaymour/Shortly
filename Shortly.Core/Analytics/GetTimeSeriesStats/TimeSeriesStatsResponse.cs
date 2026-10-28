namespace Shortly.Core.Analytics.GetTimeSeriesStats;

public sealed record TimeSeriesPoint(DateTime BucketStart, int Clicks, int UniqueClicks);

public sealed class TimeSeriesStatsResponse
{
    public string BucketType { get; init; } = string.Empty;
    public IReadOnlyList<TimeSeriesPoint> Trend { get; init; } = [];
    public IReadOnlyDictionary<int, int> ClicksByHourOfDay { get; init; } = new Dictionary<int, int>();
    public IReadOnlyDictionary<DayOfWeek, int> ClicksByDayOfWeek { get; init; } = new Dictionary<DayOfWeek, int>();
    public int PeakHour { get; init; }
    public DayOfWeek PeakDay { get; init; }
    public double AverageClicksPerDay { get; init; }
}
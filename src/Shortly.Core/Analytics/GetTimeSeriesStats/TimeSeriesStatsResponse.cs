namespace Shortly.Core.Analytics.GetTimeSeriesStats;


public sealed record TimeSeriesPoint(
    DateTime BucketStart,
    long Clicks,
    long UniqueClicks);

public sealed class TimeSeriesStatsResponse
{
    public string BucketType { get; init; } = string.Empty;

    public IReadOnlyList<TimeSeriesPoint> Trend { get; init; } = [];

    public IReadOnlyDictionary<int, long> ClicksByHourOfDay { get; init; }
        = new Dictionary<int, long>();

    public IReadOnlyDictionary<DayOfWeek, long> ClicksByDayOfWeek { get; init; }
        = new Dictionary<DayOfWeek, long>();

    public int? PeakHour { get; init; }

    public DayOfWeek? PeakDay { get; init; }

    public double AverageClicksPerDay { get; init; }
}
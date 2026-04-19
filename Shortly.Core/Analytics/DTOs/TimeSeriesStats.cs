namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Daily or weekly click trend, hourly distribution, day-of-week distribution.
///     Bucket granularity (daily vs weekly) is chosen automatically:
///     ≤ 90 days → daily, > 90 days → ISO-week.
///     <para>Endpoints:</para>
///     <para>  GET /api/statistics/urls/{id}/timeseries</para>
///     <para>  GET /api/statistics/my-stats/timeseries</para>
/// </summary>
public class TimeSeriesStats
{
    /// <summary>"daily" or "weekly" — lets the frontend label axes correctly.</summary>
    public string BucketType { get; set; } = "daily";

    public List<TimeSeriesDataPoint> Trend { get; set; } = [];
    public Dictionary<DateTime, int> DailyClicks { get; set; } = [];
    public Dictionary<int, int> HourlyDistribution { get; set; } = [];
    public Dictionary<int, int> ClicksByHourOfDay { get; set; } = [];
    public Dictionary<DayOfWeek, int> ClicksByDayOfWeek { get; set; } = [];
    public int PeakHour { get; set; }
    public DayOfWeek PeakDay { get; set; }
    public DateTime? PeakDate { get; set; }
    public int PeakDateClicks { get; set; }
    public double AverageClicksPerDay { get; set; }
    public double AverageClicksPerHour { get; set; }
}
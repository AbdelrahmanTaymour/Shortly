namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Individual time series data point
/// </summary>
public class TimeSeriesDataPoint
{
    public DateTime Timestamp { get; set; }
    public int Clicks { get; set; }
    public int UniqueClicks { get; set; }
}
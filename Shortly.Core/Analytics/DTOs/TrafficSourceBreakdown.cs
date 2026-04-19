namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Traffic source breakdown
/// </summary>
public class TrafficSourceBreakdown
{
    public string Source { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public double Percentage { get; set; }
}
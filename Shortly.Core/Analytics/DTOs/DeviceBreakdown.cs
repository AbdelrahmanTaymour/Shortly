namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Device breakdown with detailed metrics
/// </summary>
public class DeviceBreakdown
{
    public string Name { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public int UniqueUsers { get; set; }
    public double Percentage { get; set; }
}
namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Referrer domain breakdown
/// </summary>
public class ReferrerBreakdown
{
    public string Domain { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public double Percentage { get; set; }
}
namespace Shortly.Core.Analytics.DTOs;

public class OsBreakdown
{
    public string OsName { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public double Percentage { get; set; }
}
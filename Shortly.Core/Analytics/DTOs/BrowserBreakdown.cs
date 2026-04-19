namespace Shortly.Core.Analytics.DTOs;

public class BrowserBreakdown
{
    public string BrowserName { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public double Percentage { get; set; }
}
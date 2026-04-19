namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Campaign performance metrics
/// </summary>
public class CampaignPerformance
{
    public string CampaignName { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Medium { get; set; }
    public int Clicks { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public double Percentage { get; set; }
}
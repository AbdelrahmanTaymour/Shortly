namespace Shortly.Core.Analytics.DTOs;

/// <summary>Core click metrics computable without session-level grouping.</summary>
public class OverviewMetrics
{
    public long TotalClicks { get; set; }
    public long UniqueClicks { get; set; }
    public int ActiveDays { get; set; }
    public double AverageClicksPerDay { get; set; }
    public double ClickThroughRate { get; set; }
    public DateTime? FirstClickDate { get; set; }
    public DateTime? LastClickDate { get; set; }

    public long ClicksToday { get; set; }
    public long ClicksThisWeek { get; set; }
    public long ClicksThisMonth { get; set; }
}
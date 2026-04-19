namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Session-level engagement metrics.
///     Computed from a GROUP BY SessionId CTE — deferred to the Engagement
///     panel on both the link stats page and the analytics dashboard.
///     <para>Endpoints:</para>
///     <para>  GET /api/statistics/urls/{id}/engagement</para>
///     <para>  GET /api/statistics/my-stats/engagement</para>
/// </summary>
public class EngagementMetrics
{
    public double BounceRate { get; set; }
    public double ReturnVisitorRate { get; set; }
    public int NewVisitors { get; set; }
    public int ReturningVisitors { get; set; }
    public TimeSpan AverageSessionDuration { get; set; }
    public double ClicksPerSession { get; set; }
}
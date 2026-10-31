namespace Shortly.Core.Analytics.GetEngagementMetrics;

public sealed class EngagementMetricsResponse
{
    public double BounceRate { get; init; }
    public double ReturnVisitorRate { get; init; }
    public int NewVisitors { get; init; }
    public int ReturningVisitors { get; init; }
    public TimeSpan AverageSessionDuration { get; init; }
    public double ClicksPerSession { get; init; }
}
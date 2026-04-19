namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Lightweight initial payload for the analytics dashboard.
///     Populated from four cheap result sets — no GROUP BY SessionId,
///     no per-URL ranking.
///     <para>Endpoint: GET /api/statistics/my-stats/overview</para>
/// </summary>
public class UserOverview
{
    public UserOverviewMetrics Overview { get; set; } = new();
    public string? TopCountry { get; set; }
    public string? TopReferrer { get; set; }
}
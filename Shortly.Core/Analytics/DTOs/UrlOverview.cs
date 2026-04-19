namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Lightweight initial payload for a link stats page.
///     Populated from four cheap result sets — no GROUP BY SessionId.
///     <para>Endpoint: GET /api/statistics/urls/{id}/overview</para>
/// </summary>
public class UrlOverview
{
    public OverviewMetrics Overview { get; set; } = new();
    public string? TopCountry { get; set; }
    public string? TopReferrer { get; set; }
}
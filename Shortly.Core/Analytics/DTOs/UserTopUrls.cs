namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Top-10 URL ranking.
///     <para>Endpoint: GET /api/statistics/my-stats/top-urls</para>
/// </summary>
public class UserTopUrls
{
    public List<TopPerformingUrl> TopUrls { get; set; } = [];
}
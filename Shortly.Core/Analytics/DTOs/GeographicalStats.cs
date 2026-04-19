namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Country and city breakdown.
///     Top 5 of each dimension + aggregated "Other" bucket, collapsed server-side.
///     <para>Endpoints:</para>
///     <para>  GET /api/statistics/urls/{id}/geography</para>
///     <para>  GET /api/statistics/my-stats/geography</para>
/// </summary>
public class GeographicalStats
{
    public List<StatItem> TopCountries { get; set; } = [];
    public List<StatItem> TopCities { get; set; } = [];
    public int TotalCountries { get; set; }
    public int TotalCities { get; set; }
}

public record StatItem(string Name, int Clicks);
namespace Shortly.Core.Analytics.GetGeographicalStats;

public sealed record GeoBreakdown(string Name, int Clicks, double Percentage);

public sealed class GeographicalStatsResponse
{
    public IReadOnlyList<GeoBreakdown> TopCountries { get; init; } = [];
    public IReadOnlyList<GeoBreakdown> TopCities { get; init; } = [];
    public int TotalDistinctCountries { get; init; }
    public int TotalDistinctCities { get; init; }
}
namespace Shortly.Infrastructure.ClickTracking;

internal sealed class GeoLite2Options
{
    public const string SectionName = "GeoLite2";

    public string AccountId { get; init; } = string.Empty;

    public string LicenseKey { get; init; } = string.Empty;

    public string EditionId { get; init; } = "GeoLite2-City";

    public string DatabasePath { get; init; } = "data/GeoLite2-City.mmdb";

    public int UpdateIntervalDays { get; init; } = 7;
}
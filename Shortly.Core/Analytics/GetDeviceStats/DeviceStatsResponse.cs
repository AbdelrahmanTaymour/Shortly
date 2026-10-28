namespace Shortly.Core.Analytics.GetDeviceStats;

public sealed record DeviceBreakdown(string Name, int Clicks, double Percentage);

public sealed class DeviceStatsResponse
{
    public IReadOnlyList<DeviceBreakdown> TopDeviceTypes { get; init; } = [];
    public IReadOnlyList<DeviceBreakdown> TopBrowsers { get; init; } = [];
    public IReadOnlyList<DeviceBreakdown> TopOperatingSystems { get; init; } = [];
    public double MobilePercentage { get; init; }
    public double DesktopPercentage { get; init; }
    public double TabletPercentage { get; init; }
}
namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Device types (top 5), browsers (top 6), operating systems (top 6).
///     TOP N pushed into SQL — no full-cardinality result sets returned.
///     <para>Endpoints:</para>
///     <para>  GET /api/statistics/urls/{id}/devices</para>
///     <para>  GET /api/statistics/my-stats/devices</para>
/// </summary>
public class DeviceStats
{
    public List<DeviceBreakdown> TopDeviceTypes { get; set; } = [];
    public List<BrowserBreakdown> TopBrowsers { get; set; } = [];
    public List<OsBreakdown> TopOperatingSystems { get; set; } = [];
    public double MobilePercentage { get; set; }
    public double DesktopPercentage { get; set; }
    public double TabletPercentage { get; set; }
}
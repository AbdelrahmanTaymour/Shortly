using Shortly.Core.Analytics.DTOs;

namespace Shortly.Core.Analytics.Contracts;

public interface IClickStatisticsDapperQueries
{
    // ── Per-URL ───────────────────────────────────────────────────────────────
 
    Task<UrlOverview>        GetUrlOverviewAsync(long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<EngagementMetrics>  GetUrlEngagementAsync(long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<GeographicalStats>  GetUrlGeographyAsync(long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<DeviceStats>        GetUrlDeviceStatsAsync(long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<TrafficStats>       GetUrlTrafficStatsAsync(long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<TimeSeriesStats>    GetUrlTimeSeriesAsync(long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
 
    // ── Per-user ──────────────────────────────────────────────────────────────
 
    Task<UserOverview>       GetUserOverviewAsync(Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<EngagementMetrics>  GetUserEngagementAsync(Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<UserTopUrls>        GetUserTopUrlsAsync(Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<TimeSeriesStats>    GetUserTimeSeriesAsync(Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<GeographicalStats>  GetUserGeographyAsync(Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<TrafficStats>       GetUserTrafficAsync(Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<DeviceStats>        GetUserDevicesAsync(Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
}

using Shortly.Core.Analytics.DTOs;

namespace Shortly.Core.Analytics.Contracts;

/// <summary>
/// Service contract for URL and user statistics.
/// </summary>
public interface IUrlStatisticsService
{
    // ── Per-URL ───────────────────────────────────────────────────────────────
    Task<UrlOverview>       GetUrlOverviewAsync(long id, DateTime? s, DateTime? e, CancellationToken ct);
    Task<EngagementMetrics> GetUrlEngagementAsync(long id, DateTime? s, DateTime? e, CancellationToken ct);
    Task<GeographicalStats> GetUrlGeographyAsync(long id, DateTime? s, DateTime? e, CancellationToken ct);
    Task<DeviceStats>       GetUrlDevicesAsync(long id, DateTime? s, DateTime? e, CancellationToken ct);
    Task<TrafficStats>      GetUrlTrafficAsync(long id, DateTime? s, DateTime? e, CancellationToken ct);
    Task<TimeSeriesStats>   GetUrlTimeSeriesAsync(long id, DateTime? s, DateTime? e, CancellationToken ct);
 
    // ── Per-user ──────────────────────────────────────────────────────────────
    Task<UserOverview>      GetUserOverviewAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<EngagementMetrics> GetUserEngagementAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<UserTopUrls>       GetUserTopUrlsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<TimeSeriesStats>   GetUserTimeSeriesAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<GeographicalStats> GetUserGeographyAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<TrafficStats>      GetUserTrafficAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<DeviceStats>       GetUserDevicesAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}

using Shortly.Core.Analytics.CacheContracts;
using Shortly.Core.Analytics.Contracts;
using Shortly.Core.Analytics.DTOs;

namespace Shortly.Core.Analytics.Services;

/// <summary>
///     Optimized analytics service. Uses the Decorator pattern to wrap
///     <see cref="ICachedStatisticsService" /> around Dapper queries.
/// </summary>
public class UrlStatisticsService(
    IClickStatisticsDapperQueries clickDapperQueries,
    ICachedStatisticsService service
) : IUrlStatisticsService
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    private const int MaxRangeDays = 365;
    private const int DefaultLookBackDays = 30;

    // ── Per-URL endpoints (unchanged) ────────────────────────────────────────

    #region Per-URL endpoints

    public Task<UrlOverview> GetUrlOverviewAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return service.GetOrSetUrlMetricAsync("overview", id, start, end,
            _ => clickDapperQueries.GetUrlOverviewAsync(id, start, end, ct), DefaultCacheTtl, ct);
    }
    
    public Task<EngagementMetrics> GetUrlEngagementAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return service.GetOrSetUrlMetricAsync("engagement", id, start, end,
            _ => clickDapperQueries.GetUrlEngagementAsync(id, start, end, ct), DefaultCacheTtl, ct);
    }

    public Task<GeographicalStats> GetUrlGeographyAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return service.GetOrSetUrlMetricAsync("geography", id, start, end,
            _ => clickDapperQueries.GetUrlGeographyAsync(id, start, end, ct), DefaultCacheTtl, ct);
    }

    public Task<DeviceStats> GetUrlDevicesAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return service.GetOrSetUrlMetricAsync("devices", id, start, end,
            _ => clickDapperQueries.GetUrlDeviceStatsAsync(id, start, end, ct), DefaultCacheTtl, ct);
    }

    public Task<TrafficStats> GetUrlTrafficAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return service.GetOrSetUrlMetricAsync("traffic", id, start, end,
            _ => clickDapperQueries.GetUrlTrafficStatsAsync(id, start, end, ct), DefaultCacheTtl, ct);
    }

    public Task<TimeSeriesStats> GetUrlTimeSeriesAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return service.GetOrSetUrlMetricAsync("timeseries", id, start, end,
            _ => clickDapperQueries.GetUrlTimeSeriesAsync(id, start, end, ct), DefaultCacheTtl, ct);
    }

    #endregion

    // ── Per-user endpoints ───────────────────────────────────────────────────

    #region Per-user endpoints

    /// <inheritdoc />
    public async Task<UserOverview> GetUserOverviewAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return await service.GetOrSetUserMetricAsync<UserOverview>(
            "overview", userId, start, end,
            _ => clickDapperQueries.GetUserOverviewAsync(userId, start, end, cancellationToken),
            DefaultCacheTtl, cancellationToken);
    }

    public async Task<EngagementMetrics> GetUserEngagementAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return await service.GetOrSetUserMetricAsync<EngagementMetrics>(
            "engagement", userId, start, end,
            _ => clickDapperQueries.GetUserEngagementAsync(userId, start, end, cancellationToken),
            DefaultCacheTtl, cancellationToken);
    }

    public async Task<UserTopUrls> GetUserTopUrlsAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return await service.GetOrSetUserMetricAsync<UserTopUrls>(
            "topurls", userId, start, end,
            _ => clickDapperQueries.GetUserTopUrlsAsync(userId, start, end, cancellationToken),
            DefaultCacheTtl, cancellationToken);
    }
    
    public async Task<TimeSeriesStats> GetUserTimeSeriesAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return await service.GetOrSetUserMetricAsync<TimeSeriesStats>(
            "timeseries", userId, start, end,
            _ => clickDapperQueries.GetUserTimeSeriesAsync(userId, start, end, cancellationToken),
            DefaultCacheTtl, cancellationToken);
    }

    public async Task<GeographicalStats> GetUserGeographyAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return await service.GetOrSetUserMetricAsync<GeographicalStats>(
            "geography", userId, start, end,
            _ => clickDapperQueries.GetUserGeographyAsync(userId, start, end, cancellationToken),
            DefaultCacheTtl, cancellationToken);
    }

    public async Task<TrafficStats> GetUserTrafficAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return await service.GetOrSetUserMetricAsync<TrafficStats>(
            "traffic", userId, start, end,
            _ => clickDapperQueries.GetUserTrafficAsync(userId, start, end, cancellationToken),
            DefaultCacheTtl, cancellationToken);
    }

    public async Task<DeviceStats> GetUserDevicesAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return await service.GetOrSetUserMetricAsync<DeviceStats>(
            "devices", userId, start, end,
            _ => clickDapperQueries.GetUserDevicesAsync(userId, start, end, cancellationToken),
            DefaultCacheTtl, cancellationToken);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static (DateTime start, DateTime end) ClampRange(DateTime? startDate, DateTime? endDate)
    {
        var end = (endDate ?? DateTime.UtcNow).ToUniversalTime();
        var start = (startDate ?? end.AddDays(-DefaultLookBackDays)).ToUniversalTime();

        if (start > end) start = end.AddDays(-DefaultLookBackDays);

        if ((end - start).TotalDays > MaxRangeDays)
            start = end.AddDays(-MaxRangeDays);

        return (start, end);
    }
}
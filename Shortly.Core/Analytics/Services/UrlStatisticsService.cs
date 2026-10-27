using Shortly.Core.Analytics.Contracts;
using Shortly.Core.Analytics.DTOs;

namespace Shortly.Core.Analytics.Services;

/// <summary>
///     Optimized analytics service. Directly invokes Dapper queries without caching.
/// </summary>
public class UrlStatisticsService(
    IClickStatisticsDapperQueries clickDapperQueries
) : IUrlStatisticsService
{
    private const int MaxRangeDays = 365;
    private const int DefaultLookBackDays = 30;

    // ── Per-URL endpoints ────────────────────────────────────────────────────

    #region Per-URL endpoints

    public Task<UrlOverview> GetUrlOverviewAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return clickDapperQueries.GetUrlOverviewAsync(id, start, end, ct);
    }
    
    public Task<EngagementMetrics> GetUrlEngagementAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return clickDapperQueries.GetUrlEngagementAsync(id, start, end, ct);
    }

    public Task<GeographicalStats> GetUrlGeographyAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return clickDapperQueries.GetUrlGeographyAsync(id, start, end, ct);
    }

    public Task<DeviceStats> GetUrlDevicesAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return clickDapperQueries.GetUrlDeviceStatsAsync(id, start, end, ct);
    }

    public Task<TrafficStats> GetUrlTrafficAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return clickDapperQueries.GetUrlTrafficStatsAsync(id, start, end, ct);
    }

    public Task<TimeSeriesStats> GetUrlTimeSeriesAsync(long id, DateTime? s, DateTime? e, CancellationToken ct)
    {
        var (start, end) = ClampRange(s, e);
        return clickDapperQueries.GetUrlTimeSeriesAsync(id, start, end, ct);
    }

    #endregion

    // ── Per-user endpoints ───────────────────────────────────────────────────

    #region Per-user endpoints

    public Task<UserOverview> GetUserOverviewAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return clickDapperQueries.GetUserOverviewAsync(userId, start, end, cancellationToken);
    }

    public Task<EngagementMetrics> GetUserEngagementAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return clickDapperQueries.GetUserEngagementAsync(userId, start, end, cancellationToken);
    }

    public Task<UserTopUrls> GetUserTopUrlsAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return clickDapperQueries.GetUserTopUrlsAsync(userId, start, end, cancellationToken);
    }
    
    public Task<TimeSeriesStats> GetUserTimeSeriesAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return clickDapperQueries.GetUserTimeSeriesAsync(userId, start, end, cancellationToken);
    }

    public Task<GeographicalStats> GetUserGeographyAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return clickDapperQueries.GetUserGeographyAsync(userId, start, end, cancellationToken);
    }

    public Task<TrafficStats> GetUserTrafficAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return clickDapperQueries.GetUserTrafficAsync(userId, start, end, cancellationToken);
    }

    public Task<DeviceStats> GetUserDevicesAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ClampRange(startDate, endDate);
        return clickDapperQueries.GetUserDevicesAsync(userId, start, end, cancellationToken);
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
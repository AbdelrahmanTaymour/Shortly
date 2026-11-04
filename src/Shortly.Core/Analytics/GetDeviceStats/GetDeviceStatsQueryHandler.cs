using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetDeviceStats;

internal sealed class GetDeviceStatsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    AnalyticsScopeResolver scopeResolver)
    : IQueryHandler<GetDeviceStatsQuery, DeviceStatsResponse>
{
    private const int TopN = 6;

    public async Task<Result<DeviceStatsResponse>> Handle(
        GetDeviceStatsQuery request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await scopeResolver.ResolveAsync(
            request.ShortUrlId, request.From, request.To, AnalyticsFeature.GeoDeviceAnalytics, cancellationToken);
        if (scopeResult.IsFailure) return Result.Failure<DeviceStatsResponse>(scopeResult.Error);
        var scope = scopeResult.Value;

        var parameters = new
        {
            scope.ShortUrlId,
            scope.UserId,
            scope.OrganizationId,
            scope.From,
            scope.To,
            TopN
        };

        using var connection = sqlConnectionFactory.CreateConnection();

        var dimensionsSql = $"""
            WITH scoped AS (
                SELECT
                    COALESCE(NULLIF(BTRIM(ce.device_type), ''), 'Unknown') AS DeviceType,
                    COALESCE(NULLIF(BTRIM(ce.browser), ''), 'Unknown') AS Browser,
                    COALESCE(NULLIF(BTRIM(ce.operating_system), ''), 'Unknown') AS OperatingSystem
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            ),
            by_device AS (
                SELECT
                    DeviceType AS Name,
                    COUNT(*) AS Clicks,
                    ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC, DeviceType) AS Rn
                FROM scoped
                GROUP BY DeviceType
            ),
            by_browser AS (
                SELECT
                    Browser AS Name,
                    COUNT(*) AS Clicks,
                    ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC, Browser) AS Rn
                FROM scoped
                GROUP BY Browser
            ),
            by_os AS (
                SELECT
                    OperatingSystem AS Name,
                    COUNT(*) AS Clicks,
                    ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC, OperatingSystem) AS Rn
                FROM scoped
                GROUP BY OperatingSystem
            )
            SELECT 'device' AS Dimension, Name, Clicks
            FROM by_device
            WHERE Rn <= @TopN

            UNION ALL

            SELECT 'browser', Name, Clicks
            FROM by_browser
            WHERE Rn <= @TopN

            UNION ALL

            SELECT 'os', Name, Clicks
            FROM by_os
            WHERE Rn <= @TopN;
            """;

        var totalsSql = $"""
            WITH scoped AS (
                SELECT
                    COALESCE(NULLIF(BTRIM(ce.device_type), ''), 'Unknown') AS DeviceType
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            )
            SELECT
                COUNT(*) FILTER (WHERE DeviceType = 'Mobile') AS Mobile,
                COUNT(*) FILTER (WHERE DeviceType = 'Desktop') AS Desktop,
                COUNT(*) FILTER (WHERE DeviceType = 'Tablet') AS Tablet,
                COUNT(*) AS Total
            FROM scoped;
            """;

        var dimensionCommand = new CommandDefinition(
            dimensionsSql,
            parameters,
            cancellationToken: cancellationToken);

        var totalsCommand = new CommandDefinition(
            totalsSql,
            parameters,
            cancellationToken: cancellationToken);

        var rows = (await connection.QueryAsync<DimensionRow>(dimensionCommand)).AsList();

        var totals = await connection.QuerySingleAsync<DeviceTotalsRow>(totalsCommand);

        IReadOnlyList<DeviceBreakdown> Pick(string dimension)
        {
            return rows
                .Where(r => r.Dimension == dimension)
                .Select(r => new DeviceBreakdown(
                    r.Name,
                    r.Clicks,
                    totals.Total == 0 ? 0 : r.Clicks * 100.0 / totals.Total))
                .ToList();
        }

        return new DeviceStatsResponse
        {
            TopDeviceTypes = Pick("device"),
            TopBrowsers = Pick("browser"),
            TopOperatingSystems = Pick("os"),
            MobilePercentage = totals.Total == 0 ? 0 : totals.Mobile * 100.0 / totals.Total,
            DesktopPercentage = totals.Total == 0 ? 0 : totals.Desktop * 100.0 / totals.Total,
            TabletPercentage = totals.Total == 0 ? 0 : totals.Tablet * 100.0 / totals.Total
        };
    }

    private sealed class DimensionRow
    {
        public string Dimension { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int Clicks { get; init; }
    }

    private sealed class DeviceTotalsRow
    {
        public long Mobile { get; init; }
        public long Desktop { get; init; }
        public long Tablet { get; init; }
        public long Total { get; init; }
    }
}
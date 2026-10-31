using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetDeviceStats;

internal sealed class GetDeviceStatsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetDeviceStatsQuery, DeviceStatsResponse>
{
    private const int TopN = 6;

    public async Task<Result<DeviceStatsResponse>> Handle(GetDeviceStatsQuery request,
        CancellationToken cancellationToken)
    {
        var scope = new AnalyticsScope(request.ShortUrlId, request.UserId, request.OrganizationId, request.From,
            request.To);
        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
                   WITH scoped AS (
                       SELECT ce.device_type, ce.browser, ce.operating_system
                       FROM click_events ce
                       JOIN short_urls s ON s.id = ce.short_url_id
                       WHERE {scope.BuildWhereClause()}
                   ),
                   by_device AS (SELECT device_type AS name, COUNT(*) AS clicks, ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn FROM scoped GROUP BY device_type),
                   by_browser AS (SELECT browser AS name, COUNT(*) AS clicks, ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn FROM scoped GROUP BY browser),
                   by_os AS (SELECT operating_system AS name, COUNT(*) AS clicks, ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn FROM scoped GROUP BY operating_system)
                   SELECT 'device' AS dimension, name, clicks FROM by_device WHERE rn <= @TopN
                   UNION ALL SELECT 'browser', name, clicks FROM by_browser WHERE rn <= @TopN
                   UNION ALL SELECT 'os', name, clicks FROM by_os WHERE rn <= @TopN;

                   SELECT
                       COUNT(*) FILTER (WHERE device_type = 'Mobile')  AS Mobile,
                       COUNT(*) FILTER (WHERE device_type = 'Desktop') AS Desktop,
                       COUNT(*) FILTER (WHERE device_type = 'Tablet')  AS Tablet,
                       COUNT(*)                                        AS Total
                   FROM scoped
                   """;

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            request.ShortUrlId, request.UserId, request.OrganizationId, request.From, request.To, TopN
        });

        var rows = (await multi.ReadAsync<DimensionRow>()).ToList();
        var totals = await multi.ReadSingleAsync<DeviceTotalsRow>();
        var total = Math.Max(totals.Total, 1);

        IReadOnlyList<DeviceBreakdown> Pick(string dim)
        {
            var group = rows.Where(r => r.Dimension == dim).ToList();
            var sum = Math.Max(group.Sum(r => r.Clicks), 1);
            return group.Select(r => new DeviceBreakdown(r.Name, r.Clicks, r.Clicks * 100.0 / sum)).ToList();
        }
        
        return new DeviceStatsResponse
        {
            TopDeviceTypes = Pick("device"),
            TopBrowsers = Pick("browser"),
            TopOperatingSystems = Pick("os"),
            MobilePercentage = totals.Mobile * 100.0 / total,
            DesktopPercentage = totals.Desktop * 100.0 / total,
            TabletPercentage = totals.Tablet * 100.0 / total
        };
    }

    private sealed class DimensionRow
    {
        public string Dimension { get; } = "";
        public string Name { get; } = "";
        public int Clicks { get; init; }
    }

    private sealed class DeviceTotalsRow
    {
        public int Mobile { get; init; }
        public int Desktop { get; init; }
        public int Tablet { get; init; }
        public int Total { get; init; }
    }
}
using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetUrlsApproachingClickLimit;

internal sealed class GetUrlsApproachingClickLimitQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetUrlsApproachingClickLimitQuery, PagedResponse<ShortUrlResponse>>
{
    public async Task<Result<PagedResponse<ShortUrlResponse>>> Handle(
        GetUrlsApproachingClickLimitQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (long)(page - 1) * pageSize;

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT COUNT(*)
            FROM short_urls
            WHERE click_limit > 0
              AND total_clicks >= click_limit * @WarningThreshold;

            SELECT
                id AS Id,
                original_url AS OriginalUrl,
                short_code AS ShortCode,
                is_active AS IsActive,
                total_clicks AS TotalClicks,
                click_limit AS ClickLimit,
                is_private AS IsPrivate,
                is_password_protected AS IsPasswordProtected,
                expires_at AS ExpiresAt,
                title AS Title,
                description AS Description,
                created_at_utc AS CreatedAtUtc
            FROM short_urls
            WHERE click_limit > 0
              AND total_clicks >= click_limit * @WarningThreshold
            ORDER BY
                total_clicks::float8 / click_limit DESC,
                created_at_utc DESC,
                id
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                request.WarningThreshold,
                Offset = offset,
                PageSize = pageSize
            },
            cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        int totalCount = await multi.ReadSingleAsync<int>();
        var rows = (await multi.ReadAsync<ShortUrlRow>()).ToList();

        return new PagedResponse<ShortUrlResponse>
        {
            Items = rows.Select(r => r.ToResponse()).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetUrlsApproachingClickLimit;

internal sealed class GetUrlsApproachingClickLimitQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetUrlsApproachingClickLimitQuery, PagedResponse<ShortUrlResponse>>
{
    public async Task<Result<PagedResponse<ShortUrlResponse>>> Handle(GetUrlsApproachingClickLimitQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch { < 1 => 10, > 100 => 100, _ => request.PageSize };

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT id AS Id, original_url AS OriginalUrl, short_code AS ShortCode, is_active AS IsActive,
                   total_clicks AS TotalClicks, click_limit AS ClickLimit, is_private AS IsPrivate,
                   is_password_protected AS IsPasswordProtected, expires_at AS ExpiresAt,
                   title AS Title, description AS Description, created_at_utc AS CreatedAtUtc,
                   COUNT(*) OVER() AS TotalCount
            FROM short_urls
            WHERE click_limit > 0 AND total_clicks >= click_limit * @WarningThreshold
            ORDER BY (total_clicks::float8 / click_limit) DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY
            """;

        var rows = (await connection.QueryAsync<ShortUrlRow>(sql, new
        {
            request.WarningThreshold,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        })).ToList();

        if (rows.Count == 0)
            return new PagedResponse<ShortUrlResponse> { Items = [], Page = page, PageSize = pageSize, TotalCount = 0, TotalPages = 0 };

        var totalCount = rows[0].TotalCount;
        return new PagedResponse<ShortUrlResponse>
        {
            Items = rows.Select(r => r.ToResponse()).ToList(),
            Page = page, PageSize = pageSize, TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
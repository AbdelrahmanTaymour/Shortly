using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.ShortUrls.GetMyShortUrls;

internal sealed class GetMyShortUrlsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetMyShortUrlsQuery, PagedResponse<ShortUrlResponse>>
{
    public async Task<Result<PagedResponse<ShortUrlResponse>>> Handle(GetMyShortUrlsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > 50 => 50,
            _ => request.PageSize
        };

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                id AS Id, original_url AS OriginalUrl, short_code AS ShortCode, is_active AS IsActive,
                total_clicks AS TotalClicks, click_limit AS ClickLimit, is_private AS IsPrivate,
                is_password_protected AS IsPasswordProtected, expires_at AS ExpiresAt,
                title AS Title, description AS Description, created_at_utc AS CreatedAtUtc,
                COUNT(*) OVER() AS TotalCount
            FROM short_urls
            WHERE (@OrganizationId IS NULL AND user_id = @UserId)
               OR (@OrganizationId IS NOT NULL AND organization_id = @OrganizationId)
            ORDER BY created_at_utc DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY
            """;

        var rows = (await connection.QueryAsync<ShortUrlRow>(sql, new
        {
            userContext.UserId,
            userContext.OrganizationId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        })).ToList();

        if (rows.Count == 0)
        {
            return new PagedResponse<ShortUrlResponse>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var items = rows.Select(r => r.ToResponse()).ToList();
        var totalCount = rows[0].TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<ShortUrlResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}
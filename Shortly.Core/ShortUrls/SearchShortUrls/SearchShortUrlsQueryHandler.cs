using System.Text;
using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.ShortUrls.SearchShortUrls;

internal sealed class SearchShortUrlsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<SearchShortUrlsQuery, PagedResponse<ShortUrlResponse>>
{
    public async Task<Result<PagedResponse<ShortUrlResponse>>> Handle(SearchShortUrlsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > 50 => 50,
            _ => request.PageSize
        };

        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = new StringBuilder("""
            SELECT
                id AS Id, original_url AS OriginalUrl, short_code AS ShortCode, is_active AS IsActive,
                total_clicks AS TotalClicks, click_limit AS ClickLimit, is_private AS IsPrivate,
                is_password_protected AS IsPasswordProtected, expires_at AS ExpiresAt,
                title AS Title, description AS Description, created_at_utc AS CreatedAtUtc,
                COUNT(*) OVER() AS TotalCount
            FROM short_urls
            WHERE (@OrganizationId IS NULL AND user_id = @UserId)
               OR (@OrganizationId IS NOT NULL AND organization_id = @OrganizationId)
            """);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            sql.Append("""

                       AND (title ILIKE @Search OR original_url ILIKE @Search)
                       """);
        }

        switch (request.Status)
        {
            case ShortUrlStatusFilter.Active:
                sql.Append("""

                           AND is_active = TRUE
                           """);
                break;
            case ShortUrlStatusFilter.Inactive:
                sql.Append("""

                           AND is_active = FALSE
                           """);
                break;
            case ShortUrlStatusFilter.All:
            default:
                break;
        }

        switch (request.Visibility)
        {
            case ShortUrlVisibilityFilter.Public:
                sql.Append("""

                           AND is_private = FALSE
                           """);
                break;
            case ShortUrlVisibilityFilter.Private:
                sql.Append("""

                           AND is_private = TRUE
                           """);
                break;
            case ShortUrlVisibilityFilter.All:
            default:
                break;
        }

        if (request.DateFrom is not null)
        {
            sql.Append("""

                       AND created_at_utc >= @DateFrom
                       """);
        }

        if (request.DateTo is not null)
        {
            sql.Append("""

                       AND created_at_utc <= @DateTo
                       """);
        }

        sql.Append(request.SortBy switch
        {
            ShortUrlSort.Oldest => """

                                    ORDER BY created_at_utc ASC
                                    """,
            ShortUrlSort.MostClicked => """

                                         ORDER BY total_clicks DESC
                                         """,
            _ => """

                 ORDER BY created_at_utc DESC
                 """
        });

        sql.Append("""

                   OFFSET @Offset ROWS
                   FETCH NEXT @PageSize ROWS ONLY
                   """);

        var rows = (await connection.QueryAsync<ShortUrlRow>(
            sql.ToString(),
            new
            {
                userContext.UserId,
                userContext.OrganizationId,
                Search = request.Search is null ? null : $"%{request.Search}%",
                request.DateFrom,
                request.DateTo,
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
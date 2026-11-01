using System.Text;
using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Admin.SearchOrganizations;

internal sealed class SearchOrganizationsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<SearchOrganizationsQuery, PagedResponse<OrganizationSummaryResponse>>
{
    public async Task<Result<PagedResponse<OrganizationSummaryResponse>>> Handle(
        SearchOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = request.PageSize switch
        {
            < 1 => 25,
            > 100 => 100,
            _ => request.PageSize
        };

        var where = new StringBuilder("WHERE o.deleted_at IS NULL");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            where.Append(" AND o.name ILIKE @Search");
            parameters.Add("Search", $"%{request.Search.Trim()}%");
        }

        var sql = $"""
            SELECT
                o.id AS Id,
                o.name AS Name,
                o.owner_id AS OwnerId,
                u.email AS OwnerEmail,
                o.is_active AS IsActive,
                o.is_subscribed AS IsSubscribed,
                o.created_at AS CreatedAt,
                (
                    SELECT COUNT(*)
                    FROM organization_members om
                    WHERE om.organization_id = o.id
                ) AS MemberCount,
                COUNT(*) OVER() AS TotalCount
            FROM organizations o
            INNER JOIN users u ON u.id = o.owner_id
            {where}
            ORDER BY o.created_at DESC, o.id
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        using var connection = sqlConnectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        var rows = (await connection.QueryAsync<OrgRow>(command)).AsList();

        int totalCount;

        if (rows.Count > 0)
        {
            totalCount = rows[0].TotalCount;
        }
        else
        {
            var countSql = $"""
                SELECT COUNT(*)
                FROM organizations o
                INNER JOIN users u ON u.id = o.owner_id
                {where}
                """;

            var countCommand = new CommandDefinition(
                countSql,
                parameters,
                cancellationToken: cancellationToken);

            totalCount = await connection.ExecuteScalarAsync<int>(countCommand);
        }

        var items = rows.Select(r => new OrganizationSummaryResponse
        {
            Id = r.Id,
            Name = r.Name,
            OwnerId = r.OwnerId,
            OwnerEmail = r.OwnerEmail,
            IsActive = r.IsActive,
            IsSubscribed = r.IsSubscribed,
            MemberCount = r.MemberCount,
            CreatedAt = r.CreatedAt
        }).ToList();

        return new PagedResponse<OrganizationSummaryResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private sealed class OrgRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public Guid OwnerId { get; init; }
        public string OwnerEmail { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public bool IsSubscribed { get; init; }
        public DateTime CreatedAt { get; init; }
        public int MemberCount { get; init; }
        public int TotalCount { get; init; }
    }
}
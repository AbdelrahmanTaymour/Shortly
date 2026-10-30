using System.Text;
using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Admin.SearchOrganizations;

internal sealed class SearchOrganizationsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<SearchOrganizationsQuery, PagedResponse<OrganizationSummaryResponse>>
{
    public async Task<Result<PagedResponse<OrganizationSummaryResponse>>> Handle(SearchOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch { < 1 => 25, > 100 => 100, _ => request.PageSize };

        var where = new StringBuilder("WHERE o.deleted_at IS NULL");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            where.Append(" AND o.name ILIKE @Search");
            parameters.Add("Search", $"%{request.Search}%");
        }

        var sql = $"""
            SELECT o.id AS Id, o.name AS Name, o.owner_id AS OwnerId, u.email AS OwnerEmail,
                   o.is_active AS IsActive, o.is_subscribed AS IsSubscribed, o.created_at AS CreatedAt,
                   (SELECT COUNT(*) FROM organization_members om WHERE om.organization_id = o.id) AS MemberCount,
                   COUNT(*) OVER() AS TotalCount
            FROM organizations o
            JOIN users u ON u.id = o.owner_id
            {where}
            ORDER BY o.created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        using var connection = sqlConnectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<OrgRow>(sql, parameters)).ToList();

        if (rows.Count == 0)
            return new PagedResponse<OrganizationSummaryResponse> { Items = [], Page = page, PageSize = pageSize, TotalCount = 0, TotalPages = 0 };

        return new PagedResponse<OrganizationSummaryResponse>
        {
            Items = rows.Select(r => new OrganizationSummaryResponse(r.Id, r.Name, r.OwnerId, r.OwnerEmail, r.IsActive, r.IsSubscribed, r.MemberCount, r.CreatedAt)).ToList(),
            Page = page, PageSize = pageSize, TotalCount = rows[0].TotalCount,
            TotalPages = (int)Math.Ceiling(rows[0].TotalCount / (double)pageSize)
        };
    }

    private sealed class OrgRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = "";
        public Guid OwnerId { get; init; }
        public string OwnerEmail { get; init; } = "";
        public bool IsActive { get; init; }
        public bool IsSubscribed { get; init; }
        public DateTime CreatedAt { get; init; }
        public int MemberCount { get; init; }
        public int TotalCount { get; init; }
    }
}
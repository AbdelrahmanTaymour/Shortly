using System.Text;
using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.SearchUsers;

internal sealed class SearchUsersQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<SearchUsersQuery, PagedResponse<UserAdminSummaryResponse>>
{
    public async Task<Result<PagedResponse<UserAdminSummaryResponse>>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch { < 1 => 10, > 50 => 50, _ => request.PageSize };

        var where = new StringBuilder("WHERE 1 = 1");
        var parameters = new DynamicParameters();

        switch (request.Status)
        {
            case UserStatusFilter.Active: where.Append(" AND is_active = TRUE AND is_deleted = FALSE"); break;
            case UserStatusFilter.Inactive: where.Append(" AND is_active = FALSE AND is_deleted = FALSE"); break;
            case UserStatusFilter.Deleted: where.Append(" AND is_deleted = TRUE"); break;
            case UserStatusFilter.All: break;
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            where.Append(" AND (email ILIKE @Search OR username ILIKE @Search)");
            parameters.Add("Search", $"%{request.Search}%");
        }

        if (request.Role.HasValue)
        {
            where.Append(" AND role = @Role");
            parameters.Add("Role", (byte)request.Role.Value);
        }

        var sql = $"""
            SELECT id AS Id, email AS Email, username AS Username, role AS Role, is_active AS IsActive,
                   is_email_confirmed AS IsEmailConfirmed, created_at AS CreatedAtUtc, last_login_at AS LastLoginAtUtc,
                   COUNT(*) OVER() AS TotalCount
            FROM users
            {where}
            ORDER BY created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        using var connection = sqlConnectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<UserRow>(sql, parameters)).ToList();

        if (rows.Count == 0)
            return new PagedResponse<UserAdminSummaryResponse> { Items = [], Page = page, PageSize = pageSize, TotalCount = 0, TotalPages = 0 };

        var totalCount = rows[0].TotalCount;
        return new PagedResponse<UserAdminSummaryResponse>
        {
            Items = rows.Select(r => new UserAdminSummaryResponse(
                r.Id, r.Email, r.Username, r.Role, r.IsActive, r.IsEmailConfirmed, r.CreatedAtUtc, r.LastLoginAtUtc)).ToList(),
            Page = page, PageSize = pageSize, TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private sealed class UserRow
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = "";
        public string Username { get; init; } = "";
        public UserRole Role { get; init; }
        public bool IsActive { get; init; }
        public bool IsEmailConfirmed { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime? LastLoginAtUtc { get; init; }
        public int TotalCount { get; init; }
    }
}
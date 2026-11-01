using System.Text;
using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.SearchUsers;

internal sealed class SearchUsersQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<SearchUsersQuery, PagedResponse<UserAdminSummaryResponse>>
{
    public async Task<Result<PagedResponse<UserAdminSummaryResponse>>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);

        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > 50 => 50,
            _ => request.PageSize
        };

        var where = new StringBuilder();
        var parameters = new DynamicParameters();

        switch (request.Status)
        {
            case UserStatusFilter.Active:
                where.Append(" AND u.is_active = TRUE AND u.is_deleted = FALSE");
                break;

            case UserStatusFilter.Inactive:
                where.Append(" AND u.is_active = FALSE AND u.is_deleted = FALSE");
                break;

            case UserStatusFilter.Deleted:
                where.Append(" AND u.is_deleted = TRUE");
                break;

            case UserStatusFilter.All:
                break;
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            where.Append(" AND (u.email ILIKE @Search OR u.username ILIKE @Search)");
            parameters.Add("Search", $"%{request.Search.Trim()}%");
        }

        if (request.Role.HasValue)
        {
            where.Append(" AND u.role = @Role");
            parameters.Add("Role", request.Role.Value);
        }

        var sql = $"""
                   SELECT COUNT(*)
                   FROM users u
                   WHERE TRUE
                   {where};

                   SELECT
                       u.id AS Id,
                       u.email AS Email,
                       u.username AS Username,
                       u.role AS Role,
                       u.is_active AS IsActive,
                       u.is_email_confirmed AS IsEmailConfirmed,
                       u.created_at_utc AS CreatedAtUtc,
                       u.last_login_at_utc AS LastLoginAtUtc
                   FROM users u
                   WHERE TRUE
                   {where}
                   ORDER BY u.created_at_utc DESC, u.id
                   OFFSET @Offset ROWS
                   FETCH NEXT @PageSize ROWS ONLY;
                   """;

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        using var connection = sqlConnectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        using var results = await connection.QueryMultipleAsync(command);

        var totalCount = await results.ReadSingleAsync<int>();
        var rows = (await results.ReadAsync<UserRow>()).AsList();

        return new PagedResponse<UserAdminSummaryResponse>
        {
            Items = rows.Select(r => new UserAdminSummaryResponse
            {
                Id = r.Id,
                Email = r.Email,
                Username = r.Username,
                Role = r.Role,
                IsActive = r.IsActive,
                IsEmailConfirmed = r.IsEmailConfirmed,
                CreatedAtUtc = r.CreatedAtUtc,
                LastLoginAtUtc = r.LastLoginAtUtc
            }).ToList(),

            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private sealed class UserRow
    {
        public Guid Id { get; init; }

        public string Email { get; init; } = null!;

        public string Username { get; init; } = null!;

        public UserRole Role { get; init; }

        public bool IsActive { get; init; }

        public bool IsEmailConfirmed { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime? LastLoginAtUtc { get; init; }
    }
}
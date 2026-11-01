using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;

namespace Shortly.Core.Members.GetOrganizationMembers;

internal sealed class GetOrganizationMembersQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetOrganizationMembersQuery, PagedResponse<OrganizationMemberResponse>>
{
    private const string Sql = """
                               SELECT COUNT(*) FROM organization_members WHERE organization_id = @OrganizationId;

                               SELECT om.id AS Id, om.user_id AS UserId, u.username AS Username, u.email AS Email,
                                      om.role AS Role, om.is_active AS IsActive, om.joined_at AS JoinedAt
                               FROM organization_members om
                               JOIN users u ON u.id = om.user_id
                               WHERE om.organization_id = @OrganizationId
                               ORDER BY om.joined_at ASC
                               LIMIT @PageSize OFFSET @Offset;
                               """;


    public async Task<Result<PagedResponse<OrganizationMemberResponse>>> Handle(
        GetOrganizationMembersQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        var parameters = new
        {
            request.OrganizationId,
            request.PageSize,
            Offset = (request.Page - 1) * request.PageSize
        };

        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(Sql, parameters, cancellationToken: cancellationToken));
        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<OrganizationMemberRow>()).Select(r => r.ToResponse()).ToList();

        return new PagedResponse<OrganizationMemberResponse>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    private sealed class OrganizationMemberRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public int Role { get; init; }
        public bool IsActive { get; init; }
        public DateTime JoinedAt { get; init; }

        public OrganizationMemberResponse ToResponse() =>
            new(Id, UserId, Username, Email, (MemberRole)Role, IsActive, JoinedAt);
    }
}
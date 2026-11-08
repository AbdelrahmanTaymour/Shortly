using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;

namespace Shortly.Core.Members.GetOrganizationMembers;

internal sealed class GetOrganizationMembersQueryHandler(
    IOrganizationAuthorizationService organizationAuthorization,
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
        var membership = await organizationAuthorization.RequireMembershipAsync(request.OrganizationId, cancellationToken);
        if (membership.IsFailure)
            return Result.Failure<PagedResponse<OrganizationMemberResponse>>(membership.Error);

        // Same paging rules as every other list: out-of-range values are replaced, never passed to the database.
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        using var connection = sqlConnectionFactory.CreateConnection();

        var parameters = new
        {
            request.OrganizationId,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        };

        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(Sql, parameters, cancellationToken: cancellationToken));
        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<OrganizationMemberRow>()).Select(r => r.ToResponse()).ToList();

        return new PagedResponse<OrganizationMemberResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
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
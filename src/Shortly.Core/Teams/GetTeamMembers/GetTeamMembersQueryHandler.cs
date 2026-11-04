using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.GetTeamMembers;

internal sealed class GetTeamMembersQueryHandler(
    IOrganizationAuthorizationService organizationAuthorization, ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetTeamMembersQuery, PagedResponse<OrganizationTeamMemberResponse>>
{
    public async Task<Result<PagedResponse<OrganizationTeamMemberResponse>>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
    {
        const string orgLookupSql = "SELECT organization_id FROM organization_teams WHERE id = @TeamId";
        using var connection = sqlConnectionFactory.CreateConnection();
        var organizationId = await connection.QuerySingleOrDefaultAsync<Guid?>(orgLookupSql, new { request.TeamId });
        if (organizationId is null) return Result.Failure<PagedResponse<OrganizationTeamMemberResponse>>(OrganizationTeamErrors.NotFound);

        var membership = await organizationAuthorization.RequireMembershipAsync(organizationId.Value, cancellationToken);
        if (membership.IsFailure) return Result.Failure<PagedResponse<OrganizationTeamMemberResponse>>(membership.Error);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        const string sql = """
                           SELECT COUNT(*) FROM team_members WHERE team_id = @TeamId;

                           SELECT om.id AS MemberId, u.username AS Username, u.email AS Email, tm.joined_at AS JoinedAt
                           FROM team_members tm
                           JOIN organization_members om ON om.id = tm.member_id
                           JOIN users u ON u.id = om.user_id
                           WHERE tm.team_id = @TeamId
                           ORDER BY tm.joined_at, om.id
                           LIMIT @PageSize OFFSET @Offset;
                           """;
        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            sql, new { request.TeamId, PageSize = pageSize, Offset = (page - 1) * pageSize }, cancellationToken: cancellationToken));
        var totalCount = await multi.ReadSingleAsync<int>();
        var rows = (await multi.ReadAsync<OrganizationTeamMemberResponse>()).ToList();

        return new PagedResponse<OrganizationTeamMemberResponse>
        {
            Items = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
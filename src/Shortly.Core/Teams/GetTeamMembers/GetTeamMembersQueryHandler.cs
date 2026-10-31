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
    IUserContext userContext, IOrganizationMemberRepository memberRepository, ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetTeamMembersQuery, IReadOnlyList<OrganizationTeamMemberResponse>>
{
    public async Task<Result<IReadOnlyList<OrganizationTeamMemberResponse>>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
    {
        const string orgLookupSql = "SELECT organization_id FROM organization_teams WHERE id = @TeamId";
        using var connection = sqlConnectionFactory.CreateConnection();
        var organizationId = await connection.QuerySingleOrDefaultAsync<Guid?>(orgLookupSql, new { request.TeamId });
        if (organizationId is null) return Result.Failure<IReadOnlyList<OrganizationTeamMemberResponse>>(OrganizationTeamErrors.NotFound);

        if (!await memberRepository.IsMemberOfOrganizationAsync(userContext.UserId, organizationId.Value, cancellationToken))
            return Result.Failure<IReadOnlyList<OrganizationTeamMemberResponse>>(OrganizationErrors.NotAMember);

        const string sql = """
                           SELECT om.id AS MemberId, u.username AS Username, u.email AS Email, tm.joined_at AS JoinedAt
                           FROM team_members tm
                           JOIN organization_members om ON om.id = tm.member_id
                           JOIN users u ON u.id = om.user_id
                           WHERE tm.team_id = @TeamId
                           ORDER BY tm.joined_at
                           """;
        var rows = (await connection.QueryAsync<OrganizationTeamMemberResponse>(sql, new { request.TeamId })).ToList();
        return rows;
    }
}
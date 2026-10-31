using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Teams.GetTeam;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Teams.GetOrganizationTeams;

internal sealed class GetOrganizationTeamsQueryHandler(
    IUserContext userContext, IOrganizationMemberRepository memberRepository, ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetOrganizationTeamsQuery, IReadOnlyList<OrganizationTeamResponse>>
{
    public async Task<Result<IReadOnlyList<OrganizationTeamResponse>>> Handle(GetOrganizationTeamsQuery request, CancellationToken cancellationToken)
    {
        if (!await memberRepository.IsMemberOfOrganizationAsync(userContext.UserId, request.OrganizationId, cancellationToken))
            return Result.Failure<IReadOnlyList<OrganizationTeamResponse>>(OrganizationErrors.NotAMember);

        const string sql = """
                           SELECT ot.id AS Id, ot.organization_id AS OrganizationId, ot.team_manager_id AS TeamManagerId,
                                  u.username AS TeamManagerUsername, ot.name AS Name, ot.description AS Description, ot.created_at AS CreatedAt,
                                  (SELECT COUNT(*) FROM team_members tm WHERE tm.team_id = ot.id) AS MemberCount
                           FROM organization_teams ot
                           JOIN organization_members om ON om.id = ot.team_manager_id
                           JOIN users u ON u.id = om.user_id
                           WHERE ot.organization_id = @OrganizationId
                           ORDER BY ot.name
                           """;
        using var connection = sqlConnectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<OrganizationTeamResponse>(sql, new { request.OrganizationId })).ToList();
        return rows;
    }
}
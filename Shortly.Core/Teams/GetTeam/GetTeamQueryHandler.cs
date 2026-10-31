using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.GetTeam;

internal sealed class GetTeamQueryHandler(
    IUserContext userContext, IOrganizationMemberRepository memberRepository, ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetTeamQuery, OrganizationTeamResponse>
{
    public async Task<Result<OrganizationTeamResponse>> Handle(GetTeamQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT ot.id AS Id, ot.organization_id AS OrganizationId, ot.team_manager_id AS TeamManagerId,
                                  u.username AS TeamManagerUsername, ot.name AS Name, ot.description AS Description, ot.created_at AS CreatedAt,
                                  (SELECT COUNT(*) FROM team_members tm WHERE tm.team_id = ot.id) AS MemberCount
                           FROM organization_teams ot
                           JOIN organization_members om ON om.id = ot.team_manager_id
                           JOIN users u ON u.id = om.user_id
                           WHERE ot.id = @TeamId
                           """;
        using var connection = sqlConnectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<OrganizationTeamResponse>(sql, new { request.TeamId });
        if (row is null) return Result.Failure<OrganizationTeamResponse>(OrganizationTeamErrors.NotFound);

        if (!await memberRepository.IsMemberOfOrganizationAsync(userContext.UserId, row.OrganizationId, cancellationToken))
            return Result.Failure<OrganizationTeamResponse>(OrganizationErrors.NotAMember);

        return row;
    }
}
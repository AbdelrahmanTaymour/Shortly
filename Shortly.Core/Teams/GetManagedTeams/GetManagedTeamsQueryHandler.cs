using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Teams.GetOrganizationTeams;
using Shortly.Core.Teams.GetTeam;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Teams.GetManagedTeams;

internal sealed class GetManagedTeamsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetManagedTeamsQuery, IReadOnlyList<OrganizationTeamResponse>>
{
    public async Task<Result<IReadOnlyList<OrganizationTeamResponse>>> Handle(GetManagedTeamsQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT ot.id AS Id, ot.organization_id AS OrganizationId, ot.team_manager_id AS TeamManagerId,
                                  u.username AS TeamManagerUsername, ot.name AS Name, ot.description AS Description, ot.created_at AS CreatedAt,
                                  (SELECT COUNT(*) FROM team_members tm WHERE tm.team_id = ot.id) AS MemberCount
                           FROM organization_teams ot
                           JOIN organization_members om ON om.id = ot.team_manager_id
                           JOIN users u ON u.id = om.user_id
                           WHERE ot.team_manager_id = @ManagerId
                           ORDER BY ot.name
                           """;
        using var connection = sqlConnectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<OrganizationTeamResponse>(sql, new { request.ManagerId })).ToList();
        return rows;
    }
}
using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Teams.GetTeam;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Teams.GetManagedTeams;

internal sealed class GetMyManagedTeamsQueryHandler(
    IUserContext userContext,
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetMyManagedTeamsQuery, IReadOnlyList<OrganizationTeamResponse>>
{
    public async Task<Result<IReadOnlyList<OrganizationTeamResponse>>> Handle(
        GetMyManagedTeamsQuery request,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT
                               ot.id AS Id,
                               ot.organization_id AS OrganizationId,
                               ot.team_manager_id AS TeamManagerId,
                               u.username AS TeamManagerUsername,
                               ot.name AS Name,
                               ot.description AS Description,
                               ot.created_at AS CreatedAt,
                               (
                                   SELECT COUNT(*)
                                   FROM team_members tm
                                   INNER JOIN organization_members member
                                       ON member.id = tm.member_id
                                   WHERE tm.team_id = ot.id
                                     AND member.is_active = TRUE
                               ) AS MemberCount
                           FROM organization_teams ot
                           INNER JOIN organization_members om
                               ON om.id = ot.team_manager_id
                              AND om.organization_id = ot.organization_id
                           INNER JOIN users u ON u.id = om.user_id
                           INNER JOIN organizations o ON o.id = ot.organization_id
                           WHERE om.user_id = @UserId
                             AND om.is_active = TRUE
                             AND o.is_active = TRUE
                             AND o.deleted_at IS NULL
                           ORDER BY ot.name
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new { userContext.UserId },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<OrganizationTeamResponse>(command);

        return rows.AsList();
    }
}
using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Teams.GetTeamMembers;

internal sealed class GetTeamMembersQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetTeamMembersQuery, IReadOnlyList<OrganizationTeamMemberResponse>>
{
    public async Task<Result<IReadOnlyList<OrganizationTeamMemberResponse>>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT om.id AS MemberId, u.username AS Username, u.email AS Email, tm.joined_at AS JoinedAt
                           FROM team_members tm
                           JOIN organization_members om ON om.id = tm.member_id
                           JOIN users u ON u.id = om.user_id
                           WHERE tm.team_id = @TeamId
                           ORDER BY tm.joined_at
                           """;
        using var connection = sqlConnectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<OrganizationTeamMemberResponse>(sql, new { request.TeamId })).ToList();
        return rows;
    }
}
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
    IOrganizationAuthorizationService organizationAuthorization, ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetOrganizationTeamsQuery, PagedResponse<OrganizationTeamResponse>>
{
    public async Task<Result<PagedResponse<OrganizationTeamResponse>>> Handle(GetOrganizationTeamsQuery request, CancellationToken cancellationToken)
    {
        var membership = await organizationAuthorization.RequireMembershipAsync(request.OrganizationId, cancellationToken);
        if (membership.IsFailure) return Result.Failure<PagedResponse<OrganizationTeamResponse>>(membership.Error);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        const string sql = """
                           SELECT COUNT(*) FROM organization_teams WHERE organization_id = @OrganizationId;

                           SELECT ot.id AS Id, ot.organization_id AS OrganizationId, ot.team_manager_id AS TeamManagerId,
                                  u.username AS TeamManagerUsername, ot.name AS Name, ot.description AS Description, ot.created_at AS CreatedAt,
                                  (SELECT COUNT(*) FROM team_members tm WHERE tm.team_id = ot.id) AS MemberCount
                           FROM organization_teams ot
                           JOIN organization_members om ON om.id = ot.team_manager_id
                           JOIN users u ON u.id = om.user_id
                           WHERE ot.organization_id = @OrganizationId
                           ORDER BY ot.name, ot.id
                           LIMIT @PageSize OFFSET @Offset;
                           """;
        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            sql, new { request.OrganizationId, PageSize = pageSize, Offset = (page - 1) * pageSize }, cancellationToken: cancellationToken));
        var totalCount = await multi.ReadSingleAsync<int>();
        var rows = (await multi.ReadAsync<OrganizationTeamResponse>()).ToList();

        return new PagedResponse<OrganizationTeamResponse>
        {
            Items = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
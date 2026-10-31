using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Members.GetOrganizationMembers;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;

namespace Shortly.Core.Members.GetMembership;

internal sealed class GetMembershipQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetMembershipQuery, OrganizationMemberResponse>
{
    public async Task<Result<OrganizationMemberResponse>> Handle(GetMembershipQuery request,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT om.user_id AS UserId, u.username AS Username, u.email AS Email, om.role AS Role, om.is_active AS IsActive, om.joined_at AS JoinedAt
                           FROM organization_members om
                           JOIN users u ON u.id = om.user_id
                           WHERE om.organization_id = @OrganizationId AND om.user_id = @UserId
                           """;
        using var connection = sqlConnectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<OrganizationMemberResponse>(sql,
            new { request.OrganizationId, request.UserId });
        return row ?? Result.Failure<OrganizationMemberResponse>(OrganizationMemberErrors.NotFound);
    }
}
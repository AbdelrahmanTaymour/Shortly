using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Organizations.GetMyOrganizations;

internal sealed class GetMyOrganizationsQueryHandler(IUserContext userContext, ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetMyOrganizationsQuery, IReadOnlyList<MyOrganizationResponse>>
{
    public async Task<Result<IReadOnlyList<MyOrganizationResponse>>> Handle(GetMyOrganizationsQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT om.organization_id AS OrganizationId, o.name AS OrganizationName, om.role AS Role, om.is_active AS IsActive
                           FROM organization_members om
                           JOIN organizations o ON o.id = om.organization_id
                           WHERE om.user_id = @UserId AND om.is_active = TRUE AND o.deleted_at IS NULL
                           ORDER BY o.name
                           """;
        using var connection = sqlConnectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<MyOrganizationResponse>(sql, new { userContext.UserId })).ToList();
        return rows;
    }
}
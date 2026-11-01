using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;

namespace Shortly.Core.Organizations.GetMyOrganizations;

internal sealed class GetMyOrganizationsQueryHandler(
    IUserContext userContext,
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetMyOrganizationsQuery, IReadOnlyList<MyOrganizationResponse>>
{
    private const string Sql = """
                               SELECT
                                   om.organization_id AS OrganizationId,
                                   o.name AS Name,
                                   om.role AS Role,
                                   om.is_active AS IsActive
                               FROM organization_members om
                               INNER JOIN organizations o ON o.id = om.organization_id
                               WHERE om.user_id = @UserId
                                 AND om.is_active = TRUE
                                 AND o.deleted_at IS NULL
                               ORDER BY o.name;
                               """;

    public async Task<Result<IReadOnlyList<MyOrganizationResponse>>> Handle(
        GetMyOrganizationsQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<OrganizationMembershipRow>(
            new CommandDefinition(Sql, new { userContext.UserId }, cancellationToken: cancellationToken));

        return rows
            .Select(r => new MyOrganizationResponse(r.OrganizationId, r.Name, (MemberRole)r.Role, r.IsActive))
            .ToList();
    }

    private sealed class OrganizationMembershipRow
    {
        public Guid OrganizationId { get; init; }
        public string Name { get; init; } = null!;
        public int Role { get; init; }
        public bool IsActive { get; init; }
    }
}
using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Invitations.GetOrganizationInvitations;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;

namespace Shortly.Core.Invitations.GetInvitationById;

internal sealed class GetInvitationByIdQueryHandler(
    IOrganizationAuthorizationService organizationAuthorization, 
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetInvitationByIdQuery, OrganizationInvitationResponse>
{
    private const string Sql = """
                               SELECT oi.id AS Id, oi.organization_id AS OrganizationId, oi.invited_user_email AS InvitedUserEmail,
                                      oi.invited_user_role AS InvitedUserRole, u.username AS InvitedByUsername, oi.status AS Status,
                                      oi.registered_at AS RegisteredAt, oi.expires_at AS ExpiresAt, oi.created_at AS CreatedAt
                               FROM organization_invitations oi
                               JOIN users u ON u.id = oi.invited_by
                               WHERE oi.id = @InvitationId
                               """;

    public async Task<Result<OrganizationInvitationResponse>> Handle(GetInvitationByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();
        var command = new CommandDefinition(Sql, new { request.InvitationId }, cancellationToken: cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<OrganizationInvitationRow>(command);
        if (row is null) return Result.Failure<OrganizationInvitationResponse>(OrganizationInvitationErrors.NotFound);

        var caller = await organizationAuthorization.RequireRoleAsync(row.OrganizationId, MemberRole.OrgAdmin, cancellationToken);
        if (caller.IsFailure) return Result.Failure<OrganizationInvitationResponse>(caller.Error);

        return row.ToResponse();
    }
}
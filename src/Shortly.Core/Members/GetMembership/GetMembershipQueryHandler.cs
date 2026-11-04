using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Members.GetOrganizationMembers;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;

namespace Shortly.Core.Members.GetMembership;

internal sealed class GetMembershipQueryHandler(
    IOrganizationAuthorizationService organizationAuthorization,
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetMembershipQuery, OrganizationMemberResponse>
{
    private const string Sql = """
                               SELECT om.id          AS Id,
                                      om.user_id     AS UserId,
                                      u.username     AS Username,
                                      u.email        AS Email,
                                      om.role        AS Role,
                                      om.is_active   AS IsActive,
                                      om.joined_at   AS JoinedAt
                               FROM organization_members om
                               JOIN users u ON u.id = om.user_id
                               WHERE om.organization_id = @OrganizationId AND om.user_id = @UserId;
                               """;

    public async Task<Result<OrganizationMemberResponse>> Handle(
        GetMembershipQuery request, CancellationToken cancellationToken)
    {
        var membership = await organizationAuthorization.RequireMembershipAsync(request.OrganizationId, cancellationToken);
        if (membership.IsFailure)
            return Result.Failure<OrganizationMemberResponse>(membership.Error);

        using var connection = sqlConnectionFactory.CreateConnection();

        var row = await connection.QuerySingleOrDefaultAsync<OrganizationMemberRow>(
            new CommandDefinition(
                Sql,
                new { request.OrganizationId, request.UserId },
                cancellationToken: cancellationToken));

        if (row is null)
            return Result.Failure<OrganizationMemberResponse>(OrganizationMemberErrors.NotFound);

        return row.ToResponse();
    }

    private sealed class OrganizationMemberRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public int Role { get; init; }
        public bool IsActive { get; init; }
        public DateTime JoinedAt { get; init; }

        public OrganizationMemberResponse ToResponse() =>
            new(Id, UserId, Username, Email, (MemberRole)Role, IsActive, JoinedAt);
    }
}
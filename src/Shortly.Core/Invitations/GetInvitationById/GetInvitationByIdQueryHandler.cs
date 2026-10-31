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
    IUserContext userContext, IOrganizationMemberRepository memberRepository, ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetInvitationByIdQuery, OrganizationInvitationResponse>
{
    public async Task<Result<OrganizationInvitationResponse>> Handle(GetInvitationByIdQuery request, CancellationToken cancellationToken)
    {
        const string sql = """ /* same as before */ """;
        using var connection = sqlConnectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<OrganizationInvitationResponse>(sql, new { request.InvitationId });
        if (row is null) return Result.Failure<OrganizationInvitationResponse>(OrganizationInvitationErrors.NotFound);

        var caller = await memberRepository.GetByOrganizationAndUserAsync(row.OrganizationId, userContext.UserId, cancellationToken);
        if (caller is null || caller.Role < MemberRole.OrgAdmin)
            return Result.Failure<OrganizationInvitationResponse>(OrganizationInvitationErrors.InsufficientRole);

        return row;
    }
}
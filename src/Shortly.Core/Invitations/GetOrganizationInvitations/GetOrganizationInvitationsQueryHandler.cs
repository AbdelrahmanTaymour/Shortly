using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;

namespace Shortly.Core.Invitations.GetOrganizationInvitations;

internal sealed class GetOrganizationInvitationsQueryHandler(
    IUserContext userContext, IOrganizationMemberRepository memberRepository, ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetOrganizationInvitationsQuery, PagedResponse<OrganizationInvitationResponse>>
{
    public async Task<Result<PagedResponse<OrganizationInvitationResponse>>> Handle(GetOrganizationInvitationsQuery request, CancellationToken cancellationToken)
    {
        var caller = await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, userContext.UserId, cancellationToken);
        if (caller is null || caller.Role < MemberRole.OrgAdmin)
            return Result.Failure<PagedResponse<OrganizationInvitationResponse>>(OrganizationInvitationErrors.InsufficientRole);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        const string sql = """
            SELECT oi.id AS Id, oi.organization_id AS OrganizationId, oi.invited_user_email AS InvitedUserEmail,
                   oi.invited_user_role AS InvitedUserRole, u.username AS InvitedByUsername, oi.status AS Status,
                   oi.registered_at AS RegisteredAt, oi.expires_at AS ExpiresAt, oi.created_at AS CreatedAt
            FROM organization_invitations oi
            JOIN users u ON u.id = oi.invited_by
            WHERE oi.organization_id = @OrganizationId
            ORDER BY oi.created_at DESC
            OFFSET @Offset LIMIT @PageSize;

            SELECT COUNT(*) FROM organization_invitations oi WHERE oi.organization_id = @OrganizationId;
            """;

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new
        {
            request.OrganizationId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        var items = (await multi.ReadAsync<OrganizationInvitationResponse>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResponse<OrganizationInvitationResponse>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
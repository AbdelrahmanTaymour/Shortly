using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Members.GetOrganizationMembers;

internal sealed class GetOrganizationMembersQueryHandler(
    IUserContext userContext,
    IOrganizationMemberRepository memberRepository,
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetOrganizationMembersQuery, PagedResponse<OrganizationMemberResponse>>
{
    public async Task<Result<PagedResponse<OrganizationMemberResponse>>> Handle(GetOrganizationMembersQuery request,
        CancellationToken cancellationToken)
    {
        var isCallerMember =
            await memberRepository.IsMemberOfOrganizationAsync(userContext.UserId, request.OrganizationId,
                cancellationToken);
        if (!isCallerMember)
            return Result.Failure<PagedResponse<OrganizationMemberResponse>>(OrganizationErrors.NotAMember);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        const string sql = """
                           SELECT om.user_id AS UserId, u.username AS Username, u.email AS Email, om.role AS Role, om.is_active AS IsActive, om.joined_at AS JoinedAt
                           FROM organization_members om
                           JOIN users u ON u.id = om.user_id
                           WHERE om.organization_id = @OrganizationId AND om.is_active = TRUE
                           ORDER BY om.role DESC, u.username
                           OFFSET @Offset LIMIT @PageSize;

                           SELECT COUNT(*)
                           FROM organization_members om
                           WHERE om.organization_id = @OrganizationId AND om.is_active = TRUE;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new
        {
            request.OrganizationId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        var items = (await multi.ReadAsync<OrganizationMemberResponse>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResponse<OrganizationMemberResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
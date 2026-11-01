using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Core.Admin.GetOrganizationDetail;

internal sealed class GetOrganizationDetailQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetOrganizationDetailQuery, OrganizationDetailResponse>
{
    public async Task<Result<OrganizationDetailResponse>> Handle(GetOrganizationDetailQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT o.id AS Id, o.name AS Name, o.description AS Description, o.website AS Website, o.logo_url AS LogoUrl,
                   o.member_limit AS MemberLimit, o.is_active AS IsActive, o.is_subscribed AS IsSubscribed,
                   o.created_at AS CreatedAt, o.owner_id AS OwnerId, u.email AS OwnerEmail, o.subscription_plan_id AS SubscriptionPlan
            FROM organizations o
            JOIN users u ON u.id = o.owner_id
            WHERE o.id = @OrganizationId AND o.deleted_at IS NULL;

            SELECT om.user_id AS UserId, mu.username AS Username, mu.email AS Email, om.role AS Role, om.is_active AS IsActive
            FROM organization_members om
            JOIN users mu ON mu.id = om.user_id
            WHERE om.organization_id = @OrganizationId;

            SELECT t.id AS Id, t.name AS Name, COUNT(tm.member_id) AS MemberCount
            FROM organization_teams t
            LEFT JOIN team_members tm ON tm.team_id = t.id
            WHERE t.organization_id = @OrganizationId
            GROUP BY t.id, t.name;
            """;

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new { request.OrganizationId });

        var header = await multi.ReadSingleOrDefaultAsync<OrgHeaderRow>();
        if (header is null) return Result.Failure<OrganizationDetailResponse>(OrganizationErrors.NotFound);

        var members = (await multi.ReadAsync<OrganizationMemberSummary>()).ToList();
        var teams = (await multi.ReadAsync<OrganizationTeamSummary>()).ToList();

        return new OrganizationDetailResponse
        {
            Id = header.Id,
            Name = header.Name,
            Description = header.Description,
            Website = header.Website,
            LogoUrl = header.LogoUrl,
            MemberLimit = header.MemberLimit,
            IsActive = header.IsActive,
            IsSubscribed = header.IsSubscribed,
            CreatedAt = header.CreatedAt,
            OwnerId = header.OwnerId,
            OwnerEmail = header.OwnerEmail,
            SubscriptionPlan = header.SubscriptionPlan,
            Members = members,
            Teams = teams
        };
    }

    private sealed class OrgHeaderRow
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public string? Website { get; init; }
        public string? LogoUrl { get; init; }
        public int MemberLimit { get; init; }
        public bool IsActive { get; init; }
        public bool IsSubscribed { get; init; }
        public DateTime CreatedAt { get; init; }
        public Guid OwnerId { get; init; }
        public required string OwnerEmail { get; init; }
        public enSubscriptionPlan SubscriptionPlan { get; init; }
    }
}
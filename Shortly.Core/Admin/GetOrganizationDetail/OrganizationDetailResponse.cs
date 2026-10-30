using Shortly.Domain.Enums;
using Shortly.Domain.Members;

namespace Shortly.Core.Admin.GetOrganizationDetail;

public sealed record OrganizationMemberSummary(Guid UserId, string Username, string Email, MemberRole Role, bool IsActive);
public sealed record OrganizationTeamSummary(Guid Id, string Name, int MemberCount);

public sealed record OrganizationDetailResponse(
    Guid Id, string Name, string? Description, string? Website, string? LogoUrl, int MemberLimit,
    bool IsActive, bool IsSubscribed, DateTime CreatedAt, Guid OwnerId, string OwnerEmail,
    enSubscriptionPlan SubscriptionPlanId, IReadOnlyList<OrganizationMemberSummary> Members, IReadOnlyList<OrganizationTeamSummary> Teams);
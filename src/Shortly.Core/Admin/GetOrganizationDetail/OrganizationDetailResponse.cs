using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Core.Admin.GetOrganizationDetail;

public sealed record OrganizationMemberSummary
{
    public Guid UserId { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public MemberRole Role { get; init; }
    public bool IsActive { get; init; }
}

public sealed record OrganizationTeamSummary
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public int MemberCount { get; init; }
}

public sealed record OrganizationDetailResponse
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
    public required IReadOnlyList<OrganizationMemberSummary> Members { get; init; }
    public required IReadOnlyList<OrganizationTeamSummary> Teams { get; init; }
}
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Abstractions.Authentication;

internal sealed class OrganizationAuthorizationService(
    IUserContext userContext,
    IOrganizationMemberRepository memberRepository) : IOrganizationAuthorizationService
{
    // Request-scoped cache to prevent duplicate database lookups during a single HTTP request.
    private readonly Dictionary<Guid, OrganizationMember?> _cache = [];

    public async Task<Result<OrganizationMember>> RequireMembershipAsync(
        Guid organizationId, CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated)
            return Result.Failure<OrganizationMember>(OrganizationErrors.NotAMember);

        // Check the request cache first, otherwise fetch from the database and cache it
        if (!_cache.TryGetValue(organizationId, out var member))
        {
            member = await memberRepository.GetActiveByOrganizationAndUserAsync(
                organizationId, userContext.UserId, cancellationToken);
            _cache[organizationId] = member;
        }

        return member ?? Result.Failure<OrganizationMember>(OrganizationErrors.NotAMember);
    }

    public async Task<Result<OrganizationMember>> RequireRoleAsync(
        Guid organizationId, MemberRole minimumRole, CancellationToken cancellationToken)
    {
        var membership = await RequireMembershipAsync(organizationId, cancellationToken);
        if (membership.IsFailure) return membership;

        // Ensure the user's role meets or exceeds the required minimum role
        return membership.Value.Role < minimumRole
            ? Result.Failure<OrganizationMember>(OrganizationMemberErrors.InsufficientRole)
            : membership;
    }

    public async Task<Result<ClaimedMembership>> ResolveClaimedMembershipAsync(CancellationToken cancellationToken)
    {
        if (userContext.OrganizationId is not { } organizationId)
            return ClaimedMembership.Personal;

        var membership = await RequireMembershipAsync(organizationId, cancellationToken);
        return membership.IsFailure
            ? Result.Failure<ClaimedMembership>(membership.Error)
            : new ClaimedMembership(membership.Value);
    }
}

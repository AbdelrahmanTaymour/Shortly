using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;

namespace Shortly.Core.Abstractions.Authentication;

/// <summary>
/// Verifies organization membership against the database to prevent stale JWT claims.
/// Results are cached for the lifetime of the HTTP request.
/// </summary>
/// <summary>The verified organization membership behind a token claim, or the personal workspace when there is none.</summary>
/// <param name="Member">The active membership, or <c>null</c> when the caller works in their personal workspace.</param>
public sealed record ClaimedMembership(OrganizationMember? Member)
{
    public static readonly ClaimedMembership Personal = new((OrganizationMember?)null);
}

public interface IOrganizationAuthorizationService
{
    /// <summary>Requires the caller to be an active member of the specified organization.</summary>
    Task<Result<OrganizationMember>> RequireMembershipAsync(Guid organizationId, CancellationToken cancellationToken);

    /// <summary>Requires the caller to be an active member holding at least the specified role.</summary>
    Task<Result<OrganizationMember>> RequireRoleAsync(Guid organizationId, MemberRole minimumRole, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves and verifies the organization context from the token claims or active session.
    /// A caller in the personal workspace succeeds with <see cref="ClaimedMembership.Personal"/>.
    /// </summary>
    Task<Result<ClaimedMembership>> ResolveClaimedMembershipAsync(CancellationToken cancellationToken);
}

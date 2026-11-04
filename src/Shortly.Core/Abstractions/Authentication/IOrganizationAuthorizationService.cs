using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;

namespace Shortly.Core.Abstractions.Authentication;

/// <summary>
/// Verifies organization membership against the database to prevent stale JWT claims.
/// Results are cached for the lifetime of the HTTP request.
/// </summary>
public interface IOrganizationAuthorizationService
{
    /// <summary>Requires the caller to be an active member of the specified organization.</summary>
    Task<Result<OrganizationMember>> RequireMembershipAsync(Guid organizationId, CancellationToken cancellationToken);

    /// <summary>Requires the caller to be an active member holding at least the specified role.</summary>
    Task<Result<OrganizationMember>> RequireRoleAsync(Guid organizationId, MemberRole minimumRole, CancellationToken cancellationToken);

    /// <summary>Resolves and verifies the organization context from the token claims or active session.</summary>
    Task<Result<OrganizationMember?>> ResolveClaimedMembershipAsync(CancellationToken cancellationToken);
}

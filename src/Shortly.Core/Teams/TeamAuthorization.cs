using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams;

/// <summary>Centralized authorization policy helper for team management permissions.</summary>
internal static class TeamAuthorization
{
    /// <summary>
    /// Verifies that the caller is an active member who either manages the team or holds organization admin privileges.
    /// Membership is always resolved dynamically from the database, never trusted from tokens.
    /// </summary>
    public static async Task<Result<OrganizationMember>> RequireAuthorityAsync(
        IOrganizationAuthorizationService organizationAuthorization, 
        OrganizationTeam team, 
        CancellationToken cancellationToken)
    {
        var membership = await organizationAuthorization
            .RequireMembershipAsync(team.OrganizationId, cancellationToken);
        
        if (membership.IsFailure) 
            return membership;

        // Allow an organization admin/owner, or the assigned team manager as long as they still hold the Team Manager role.
        var isManagerOfThisTeam = membership.Value.Id == team.TeamManagerId && membership.Value.Role >= MemberRole.TeamManager;
        return isManagerOfThisTeam || membership.Value.Role >= MemberRole.OrgAdmin
            ? membership
            : Result.Failure<OrganizationMember>(OrganizationMemberErrors.InsufficientRole);
    }
}
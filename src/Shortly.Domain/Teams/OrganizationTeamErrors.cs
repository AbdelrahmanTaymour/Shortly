using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Teams;

public static class OrganizationTeamErrors
{
    public static readonly Error NotFound = Error.NotFound("OrganizationTeam.NotFound", "Team not found.");
    public static readonly Error NameTaken = Error.Conflict("OrganizationTeam.NameTaken", "A team with this name already exists in the organization.");
    public static readonly Error ManagerNotOrgMember = Error.Validation("OrganizationTeam.ManagerNotOrgMember", "The team manager must be a member of the organization.");
    public static readonly Error MemberNotOrgMember = Error.Validation("OrganizationTeam.MemberNotOrgMember", "The user must be a member of the organization before joining a team.");
    public static readonly Error AlreadyTeamMember = Error.Conflict("OrganizationTeam.AlreadyTeamMember", "User is already a member of this team.");
    public static readonly Error MemberNotFound = Error.NotFound("OrganizationTeam.MemberNotFound", "Team membership not found.");
    public static readonly Error CannotRemoveManager = Error.Validation("OrganizationTeam.CannotRemoveManager", "The team manager cannot be removed from the team — change the manager first.");
    public static readonly Error InsufficientRole = Error.Forbidden("OrganizationTeam.InsufficientRole", "You don't have sufficient permissions over this team.");
}
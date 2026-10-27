namespace Shortly.Domain.Members;

/// <summary>
/// Organization-scoped role. Stored directly on <c>OrganizationMember.Role</c>. Drives permissions
/// that only make sense within an organization (teams, invitations, org settings, org billing).
/// </summary>
public enum MemberRole : byte
{
    Viewer = 1,
    Member = 2,
    TeamManager = 3,
    OrgAdmin = 4,
    OrgOwner = 5
}
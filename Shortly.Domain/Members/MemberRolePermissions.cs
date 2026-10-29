using Shortly.Domain.Enums;

namespace Shortly.Domain.Members;

/// <summary>
/// Pure, static, in-memory lookup of an organization-member role → permission bitmask.
/// The only place <see cref="MemberRole"/> is ever translated into <see cref="enPermissions"/>.
/// </summary>
public static class MemberRolePermissions
{
    private static readonly Dictionary<MemberRole, enPermissions> Map = new()
    {
        [MemberRole.Viewer] =
            enPermissions.ReadOwnAnalytics | enPermissions.ReadOrgAnalytics | enPermissions.ReadTeamAnalytics,
        [MemberRole.Member] = enPermissions.TeamMember,
        [MemberRole.TeamManager] = enPermissions.TeamManager,
        [MemberRole.OrgAdmin] = enPermissions.OrgAdmin,
        [MemberRole.OrgOwner] = enPermissions.OrgOwner
    };

    public static enPermissions For(MemberRole role) =>
        Map.TryGetValue(role, out var permissions) ? permissions : enPermissions.None;
}
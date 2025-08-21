using Shortly.Domain.Enums;

namespace Shortly.Domain.Configuration;

// TODO: REVISIT THIS AFTER BUILDING ALL FEATURE
public class RoleConfiguration
{
    public enUserRole Role { get; set; }
    public enPermissions DefaultPermissions { get; set; }
    public string Description { get; set; }

    public static readonly Dictionary<enUserRole, RoleConfiguration> Roles = new()
    {
        [enUserRole.Viewer] = new RoleConfiguration
        {
            Role = enUserRole.Viewer,
            DefaultPermissions = enPermissions.ReadOwnAnalytics | enPermissions.ReadOrgAnalytics | enPermissions.ReadTeamAnalytics,
            Description = "Read-only access to URLs and analytics. Typically for guests or clients."
        },
        [enUserRole.Member] = new RoleConfiguration
        {
            Role = enUserRole.Member,
            DefaultPermissions = enPermissions.TeamMember,
            Description = "Experienced user with extended capabilities including analytics, branding, and campaigns."
        },
        [enUserRole.TeamManager] = new RoleConfiguration
        {
            Role = enUserRole.TeamManager,
            DefaultPermissions = enPermissions.TeamManager,
            Description = "Leads a team. Manages team roles, URLs, and integrations."
        },
        [enUserRole.OrgAdmin] = new RoleConfiguration
        {
            Role = enUserRole.OrgAdmin,
            DefaultPermissions = enPermissions.OrgAdmin,
            Description = "SuperAdmin-level access within the organization. Full team/org control and feature access."
        },
        [enUserRole.OrgOwner] = new RoleConfiguration
        {
            Role = enUserRole.OrgOwner,
            DefaultPermissions = enPermissions.OrgOwner,
            Description = "Organization owner with full organizational control"
        },
        [enUserRole.SuperAdmin] = new RoleConfiguration
        {
            Role = enUserRole.SuperAdmin,
            DefaultPermissions = enPermissions.SuperAdmin,
        },
        [enUserRole.SystemAdmin] = new RoleConfiguration
        {
            Role = enUserRole.SystemAdmin,
            DefaultPermissions = enPermissions.SystemAdmin,
            Description = "System administrator with complete access"
        }
    };
}
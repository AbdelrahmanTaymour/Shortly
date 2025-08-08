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
            DefaultPermissions = enPermissions.ReadUrl | enPermissions.ViewBasicAnalytics,
            Description = "Read-only access to URLs and analytics. Typically for guests or clients."
        },
        [enUserRole.Member] = new RoleConfiguration
        {
            Role = enUserRole.Member,
            DefaultPermissions = enPermissions.FullUrlManagement | enPermissions.FullAnalytics |
                                 enPermissions.CustomizationFeatures |
                                 enPermissions.SetPasswordProtection,
            Description = "Experienced user with extended capabilities including analytics, branding, and campaigns."
        },
        [enUserRole.TeamManager] = new RoleConfiguration
        {
            Role = enUserRole.TeamManager,
            DefaultPermissions = enPermissions.FullUrlManagement | enPermissions.FullAnalytics |
                                 enPermissions.CustomizationFeatures | enPermissions.TeamManagement,
            Description = "Leads a team. Manages team roles, URLs, and integrations."
        },
        [enUserRole.OrgAdmin] = new RoleConfiguration
        {
            Role = enUserRole.OrgAdmin,
            DefaultPermissions = enPermissions.FullUrlManagement | enPermissions.FullAnalytics |
                                 enPermissions.CustomizationFeatures | enPermissions.FullTeamAndOrg,
            Description = "Admin-level access within the organization. Full team/org control and feature access."
        },
        [enUserRole.OrgOwner] = new RoleConfiguration
        {
            Role = enUserRole.OrgOwner,
            DefaultPermissions = enPermissions.AllPermissions,
            Description = "Organization owner with full organizational control"
        },
        [enUserRole.Admin] = new RoleConfiguration
        {
            Role = enUserRole.Admin,
            DefaultPermissions = enPermissions.AllPermissions,
        },
        [enUserRole.SuperAdmin] = new RoleConfiguration
        {
            Role = enUserRole.SuperAdmin,
            DefaultPermissions = enPermissions.AllPermissions,
            Description = "System administrator with complete access"
        }
    };
}
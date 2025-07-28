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
        [enUserRole.StandardUser] = new RoleConfiguration
        {
            Role = enUserRole.StandardUser,
            DefaultPermissions = enPermissions.BasicUrlOperations | enPermissions.BasicAnalytics |
                                 enPermissions.CreateCustomAlias | enPermissions.GenerateQrCodes,
            Description = "Regular user with control over their own URLs and basic analytics."
        },
        [enUserRole.PowerUser] = new RoleConfiguration
        {
            Role = enUserRole.PowerUser,
            DefaultPermissions = enPermissions.FullUrlManagement | enPermissions.FullAnalytics |
                                 enPermissions.CustomizationFeatures | enPermissions.ApiUser |
                                 enPermissions.SetPasswordProtection | enPermissions.ManageCampaigns,
            Description = "Experienced user with extended capabilities including analytics, branding, and campaigns."
        },
        [enUserRole.TeamManager] = new RoleConfiguration
        {
            Role = enUserRole.TeamManager,
            DefaultPermissions = enPermissions.FullUrlManagement | enPermissions.FullAnalytics |
                                 enPermissions.CustomizationFeatures | enPermissions.TeamManagement |
                                 enPermissions.ApiFullAccess | enPermissions.ManageCampaigns |
                                 enPermissions.ConfigureWebhooks,
            Description = "Leads a team. Manages team roles, URLs, and integrations."
        },
        [enUserRole.OrgAdmin] = new RoleConfiguration
        {
            Role = enUserRole.OrgAdmin,
            DefaultPermissions = enPermissions.FullUrlManagement | enPermissions.FullAnalytics |
                                 enPermissions.CustomizationFeatures | enPermissions.FullTeamAndOrg |
                                 enPermissions.ApiFullAccess | enPermissions.ManageCampaigns |
                                 enPermissions.ConfigureWebhooks | enPermissions.ManageIntegrations,
            Description = "Admin-level access within the organization. Full team/org control and feature access."
        },
        [enUserRole.OrgOwner] = new RoleConfiguration
        {
            Role = enUserRole.OrgOwner,
            DefaultPermissions = enPermissions.AllPermissions & ~enPermissions.SystemAdmin,
            Description = "Organization owner with full organizational control"
        },
        [enUserRole.Admin] = new RoleConfiguration
        {
            Role = enUserRole.Admin,
            DefaultPermissions = enPermissions.AllPermissions & ~enPermissions.SystemAdmin,
        },
        [enUserRole.SuperAdmin] = new RoleConfiguration
        {
            Role = enUserRole.SuperAdmin,
            DefaultPermissions = enPermissions.AllPermissions,
            Description = "System administrator with complete access"
        }
    };
}
using Shortly.Domain.Enums;

namespace Shortly.Domain.Configuration;

public class PlanConfiguration
{
    public enSubscriptionPlan Plan { get; set; }
    public enPermissions AllowedPermissions { get; set; }
    public Dictionary<string, int> Limits { get; set; } = new();

    public static readonly Dictionary<enSubscriptionPlan, PlanConfiguration> Plans = new()
    {
        [enSubscriptionPlan.Free] = new PlanConfiguration
        {
            Plan = enSubscriptionPlan.Free,
            AllowedPermissions = enPermissions.BasicUrlOperations | enPermissions.BasicAnalytics | enPermissions.GenerateQrCodes,
            Limits = new Dictionary<string, int>
                { ["urls_per_month"] = 500, ["team_members"] = 1, ["custom_domains"] = 0 }
        },
        [enSubscriptionPlan.Starter] = new PlanConfiguration
        {
            Plan = enSubscriptionPlan.Starter,
            AllowedPermissions = enPermissions.AdvancedFeatures | enPermissions.DetailedAnalytics |
                                 enPermissions.CreateCustomAlias | enPermissions.GenerateQrCodes |
                                 enPermissions.SetPasswordProtection | enPermissions.SetLinkExpiration 
                                 | enPermissions.ApiUser,
            Limits = new Dictionary<string, int>
                { ["urls_per_month"] = 10000, ["team_members"] = 5, ["custom_domains"] = 1 }
        },
        [enSubscriptionPlan.Professional] = new PlanConfiguration
        {
            Plan = enSubscriptionPlan.Professional,
            AllowedPermissions = enPermissions.FullUrlManagement | enPermissions.FullAnalytics |
                                 enPermissions.CustomizationFeatures |
                                 enPermissions.ApiUser | enPermissions.ManageCampaigns |
                                 enPermissions.ConfigureWebhooks,
            Limits = new Dictionary<string, int>
                { ["urls_per_month"] = 100000, ["team_members"] = 25, ["custom_domains"] = 5 }
        },
        [enSubscriptionPlan.Enterprise] = new PlanConfiguration
        {
            Plan = enSubscriptionPlan.Enterprise,
            AllowedPermissions = enPermissions.AllPermissions & ~enPermissions.SystemAdmin, // All except system admin
            Limits = new Dictionary<string, int>
                { ["urls_per_month"] = -1, ["team_members"] = -1, ["custom_domains"] = -1 }
        }
    };
}
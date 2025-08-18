using Shortly.Domain.Enums;

namespace Shortly.Domain.Configuration;

public class PlanConfiguration
{
    public enum enPlanLimits
    {
        UrlsPerMonth = 1,
        QrCodesPerMonth,
        CustomShortCode
    }

    public enSubscriptionPlan Plan { get; set; }
    public enPermissions AllowedPermissions { get; set; }
    public decimal Price { get; set; }
    public Dictionary<enPlanLimits, int> Limits { get; set; } = new();

    public static readonly Dictionary<enSubscriptionPlan, PlanConfiguration> Plans = new()
    {
        [enSubscriptionPlan.Free] = new PlanConfiguration
        {
            Plan = enSubscriptionPlan.Free,
            AllowedPermissions = enPermissions.BasicUrlOperations |
                                 enPermissions.BasicAnalytics |
                                 enPermissions.GenerateQrCodes,
            Price = 0,
            Limits = new Dictionary<enPlanLimits, int>
            {
                [enPlanLimits.UrlsPerMonth] = 5,
                [enPlanLimits.QrCodesPerMonth] = 2,
                [enPlanLimits.CustomShortCode] = 0
            }
        },
        [enSubscriptionPlan.Starter] = new PlanConfiguration
        {
            Plan = enSubscriptionPlan.Starter,
            AllowedPermissions = enPermissions.BasicUrlOperations |
                                 enPermissions.BasicAnalytics |
                                 enPermissions.GenerateQrCodes |
                                 enPermissions.CreateCustomAlias,
            Price = 10,
            Limits = new Dictionary<enPlanLimits, int>
            {
                [enPlanLimits.UrlsPerMonth] = 100,
                [enPlanLimits.QrCodesPerMonth] = 5,
                [enPlanLimits.CustomShortCode] = 1
            }
        },
        [enSubscriptionPlan.Professional] = new PlanConfiguration
        {
            Plan = enSubscriptionPlan.Professional,
            AllowedPermissions = enPermissions.FullUrlManagement |
                                 enPermissions.FullAnalytics |
                                 enPermissions.CustomizationFeatures |
                                 enPermissions.BulkCreateUrl |
                                 enPermissions.SetPasswordProtection |
                                 enPermissions.SetLinkExpiration,
            Price = 50,
            Limits = new Dictionary<enPlanLimits, int>
            {
                [enPlanLimits.UrlsPerMonth] = 500,
                [enPlanLimits.QrCodesPerMonth] = 10,
                [enPlanLimits.CustomShortCode] = 3
            }
        },
        [enSubscriptionPlan.Enterprise] = new PlanConfiguration
        {
            Plan = enSubscriptionPlan.Enterprise,
            AllowedPermissions = enPermissions.AllPermissions, // All except system admin
            Price = 200,
            Limits = new Dictionary<enPlanLimits, int>
            {
                [enPlanLimits.UrlsPerMonth] = -1,
                [enPlanLimits.QrCodesPerMonth] = -1,
                [enPlanLimits.CustomShortCode] = -1
            }
        }
    };
}
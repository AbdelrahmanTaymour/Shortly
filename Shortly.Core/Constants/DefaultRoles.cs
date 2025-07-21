namespace Shortly.Core.Constants;

/// <summary>
/// Default system roles for the URL shortening service
/// </summary>
public static class DefaultRoles
{
    // System Roles
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Moderator = "Moderator";
    
    // User Roles
    public const string User = "User";
    public const string PremiumUser = "PremiumUser";
    public const string EnterpriseUser = "EnterpriseUser";
    
    // Special Roles
    public const string ApiUser = "ApiUser";
    public const string ReadOnlyUser = "ReadOnlyUser";
    public const string Guest = "Guest";
    
    /// <summary>
    /// Get all default role names
    /// </summary>
    public static readonly string[] AllRoles = 
    {
        SuperAdmin, Admin, Moderator, User, PremiumUser, 
        EnterpriseUser, ApiUser, ReadOnlyUser, Guest
    };
    
    /// <summary>
    /// Get role descriptions
    /// </summary>
    public static readonly Dictionary<string, string> RoleDescriptions = new()
    {
        { SuperAdmin, "Full system access with all administrative privileges" },
        { Admin, "Administrative access to manage users, settings, and system configuration" },
        { Moderator, "Content moderation and user management capabilities" },
        { User, "Standard user with basic URL shortening capabilities" },
        { PremiumUser, "Premium user with enhanced features and analytics" },
        { EnterpriseUser, "Enterprise user with advanced features and custom domains" },
        { ApiUser, "API-only access for programmatic integration" },
        { ReadOnlyUser, "Read-only access to URLs and analytics" },
        { Guest, "Limited guest access for demonstration purposes" }
    };
    
    /// <summary>
    /// Get role priorities (higher number = higher priority)
    /// </summary>
    public static readonly Dictionary<string, int> RolePriorities = new()
    {
        { SuperAdmin, 1000 },
        { Admin, 900 },
        { Moderator, 800 },
        { EnterpriseUser, 700 },
        { PremiumUser, 600 },
        { User, 500 },
        { ApiUser, 400 },
        { ReadOnlyUser, 300 },
        { Guest, 100 }
    };
    
    /// <summary>
    /// Get role colors for UI display
    /// </summary>
    public static readonly Dictionary<string, string> RoleColors = new()
    {
        { SuperAdmin, "#FF0000" },    // Red
        { Admin, "#FF6600" },         // Orange
        { Moderator, "#FFD700" },     // Gold
        { EnterpriseUser, "#800080" }, // Purple
        { PremiumUser, "#4169E1" },    // Royal Blue
        { User, "#008000" },          // Green
        { ApiUser, "#00CED1" },       // Dark Turquoise
        { ReadOnlyUser, "#808080" },   // Gray
        { Guest, "#C0C0C0" }          // Silver
    };
}

/// <summary>
/// Default system permissions for the URL shortening service
/// </summary>
public static class DefaultPermissions
{
    // URL Management Permissions
    public const string CreateUrls = "urls.create";
    public const string ReadUrls = "urls.read";
    public const string UpdateUrls = "urls.update";
    public const string DeleteUrls = "urls.delete";
    public const string ManageOwnUrls = "urls.manage_own";
    public const string ManageAllUrls = "urls.manage_all";
    public const string CreateCustomUrls = "urls.create_custom";
    public const string CreateBulkUrls = "urls.create_bulk";
    public const string ExportUrls = "urls.export";
    
    // Analytics Permissions
    public const string ViewAnalytics = "analytics.view";
    public const string ViewDetailedAnalytics = "analytics.view_detailed";
    public const string ViewOwnAnalytics = "analytics.view_own";
    public const string ViewAllAnalytics = "analytics.view_all";
    public const string ExportAnalytics = "analytics.export";
    public const string ViewRealTimeAnalytics = "analytics.realtime";
    
    // User Management Permissions
    public const string CreateUsers = "users.create";
    public const string ReadUsers = "users.read";
    public const string UpdateUsers = "users.update";
    public const string DeleteUsers = "users.delete";
    public const string ManageUserRoles = "users.manage_roles";
    public const string ViewUserActivity = "users.view_activity";
    public const string ImpersonateUsers = "users.impersonate";
    
    // Profile Management Permissions
    public const string UpdateOwnProfile = "profile.update_own";
    public const string ChangeOwnPassword = "profile.change_password";
    public const string ManageOwnApiKeys = "profile.manage_api_keys";
    public const string ManageOwnSessions = "profile.manage_sessions";
    
    // Domain Management Permissions
    public const string CreateCustomDomains = "domains.create";
    public const string ManageOwnDomains = "domains.manage_own";
    public const string ManageAllDomains = "domains.manage_all";
    public const string VerifyDomains = "domains.verify";
    
    // Webhook Permissions
    public const string CreateWebhooks = "webhooks.create";
    public const string ManageOwnWebhooks = "webhooks.manage_own";
    public const string ManageAllWebhooks = "webhooks.manage_all";
    public const string ViewWebhookLogs = "webhooks.view_logs";
    
    // Subscription Permissions
    public const string ManageOwnSubscription = "subscription.manage_own";
    public const string ManageAllSubscriptions = "subscription.manage_all";
    public const string ViewBilling = "billing.view";
    public const string ProcessPayments = "billing.process_payments";
    
    // System Administration Permissions
    public const string ViewSystemSettings = "admin.view_settings";
    public const string ManageSystemSettings = "admin.manage_settings";
    public const string ViewSystemLogs = "admin.view_logs";
    public const string ManageSystemLogs = "admin.manage_logs";
    public const string ViewSystemHealth = "admin.view_health";
    public const string ManageBackups = "admin.manage_backups";
    public const string ManageCache = "admin.manage_cache";
    
    // Role & Permission Management
    public const string ViewRoles = "roles.view";
    public const string ManageRoles = "roles.manage";
    public const string ViewPermissions = "permissions.view";
    public const string ManagePermissions = "permissions.manage";
    public const string AssignRoles = "roles.assign";
    
    // Security Permissions
    public const string ViewSecurityLogs = "security.view_logs";
    public const string ManageSecuritySettings = "security.manage_settings";
    public const string BanUsers = "security.ban_users";
    public const string ReviewSuspiciousActivity = "security.review_activity";
    
    // API Permissions
    public const string UseApi = "api.use";
    public const string UseAdvancedApi = "api.use_advanced";
    public const string ManageApiKeys = "api.manage_keys";
    public const string ViewApiLogs = "api.view_logs";
    
    /// <summary>
    /// Permission categories for organization
    /// </summary>
    public static readonly Dictionary<string, string> PermissionCategories = new()
    {
        // URL Management
        { CreateUrls, "URLs" },
        { ReadUrls, "URLs" },
        { UpdateUrls, "URLs" },
        { DeleteUrls, "URLs" },
        { ManageOwnUrls, "URLs" },
        { ManageAllUrls, "URLs" },
        { CreateCustomUrls, "URLs" },
        { CreateBulkUrls, "URLs" },
        { ExportUrls, "URLs" },
        
        // Analytics
        { ViewAnalytics, "Analytics" },
        { ViewDetailedAnalytics, "Analytics" },
        { ViewOwnAnalytics, "Analytics" },
        { ViewAllAnalytics, "Analytics" },
        { ExportAnalytics, "Analytics" },
        { ViewRealTimeAnalytics, "Analytics" },
        
        // User Management
        { CreateUsers, "User Management" },
        { ReadUsers, "User Management" },
        { UpdateUsers, "User Management" },
        { DeleteUsers, "User Management" },
        { ManageUserRoles, "User Management" },
        { ViewUserActivity, "User Management" },
        { ImpersonateUsers, "User Management" },
        
        // Profile
        { UpdateOwnProfile, "Profile" },
        { ChangeOwnPassword, "Profile" },
        { ManageOwnApiKeys, "Profile" },
        { ManageOwnSessions, "Profile" },
        
        // Domains
        { CreateCustomDomains, "Domains" },
        { ManageOwnDomains, "Domains" },
        { ManageAllDomains, "Domains" },
        { VerifyDomains, "Domains" },
        
        // Webhooks
        { CreateWebhooks, "Webhooks" },
        { ManageOwnWebhooks, "Webhooks" },
        { ManageAllWebhooks, "Webhooks" },
        { ViewWebhookLogs, "Webhooks" },
        
        // Billing
        { ManageOwnSubscription, "Billing" },
        { ManageAllSubscriptions, "Billing" },
        { ViewBilling, "Billing" },
        { ProcessPayments, "Billing" },
        
        // Administration
        { ViewSystemSettings, "Administration" },
        { ManageSystemSettings, "Administration" },
        { ViewSystemLogs, "Administration" },
        { ManageSystemLogs, "Administration" },
        { ViewSystemHealth, "Administration" },
        { ManageBackups, "Administration" },
        { ManageCache, "Administration" },
        
        // Roles & Permissions
        { ViewRoles, "Roles & Permissions" },
        { ManageRoles, "Roles & Permissions" },
        { ViewPermissions, "Roles & Permissions" },
        { ManagePermissions, "Roles & Permissions" },
        { AssignRoles, "Roles & Permissions" },
        
        // Security
        { ViewSecurityLogs, "Security" },
        { ManageSecuritySettings, "Security" },
        { BanUsers, "Security" },
        { ReviewSuspiciousActivity, "Security" },
        
        // API
        { UseApi, "API" },
        { UseAdvancedApi, "API" },
        { ManageApiKeys, "API" },
        { ViewApiLogs, "API" }
    };
    
    /// <summary>
    /// Get all permission names
    /// </summary>
    public static readonly string[] AllPermissions = PermissionCategories.Keys.ToArray();
    
    /// <summary>
    /// Default role-permission mappings
    /// </summary>
    public static readonly Dictionary<string, string[]> DefaultRolePermissions = new()
    {
        {
            DefaultRoles.SuperAdmin,
            AllPermissions // SuperAdmin gets all permissions
        },
        {
            DefaultRoles.Admin,
            new[]
            {
                // URL Management
                CreateUrls, ReadUrls, UpdateUrls, DeleteUrls, ManageAllUrls, CreateCustomUrls, CreateBulkUrls, ExportUrls,
                // Analytics
                ViewAnalytics, ViewDetailedAnalytics, ViewAllAnalytics, ExportAnalytics, ViewRealTimeAnalytics,
                // User Management
                CreateUsers, ReadUsers, UpdateUsers, DeleteUsers, ManageUserRoles, ViewUserActivity,
                // Profile
                UpdateOwnProfile, ChangeOwnPassword, ManageOwnApiKeys, ManageOwnSessions,
                // Domains
                CreateCustomDomains, ManageAllDomains, VerifyDomains,
                // Webhooks
                CreateWebhooks, ManageAllWebhooks, ViewWebhookLogs,
                // Billing
                ManageAllSubscriptions, ViewBilling, ProcessPayments,
                // Administration
                ViewSystemSettings, ManageSystemSettings, ViewSystemLogs, ViewSystemHealth, ManageCache,
                // Roles & Permissions
                ViewRoles, ManageRoles, ViewPermissions, AssignRoles,
                // Security
                ViewSecurityLogs, ManageSecuritySettings, BanUsers, ReviewSuspiciousActivity,
                // API
                UseApi, UseAdvancedApi, ViewApiLogs
            }
        },
        {
            DefaultRoles.Moderator,
            new[]
            {
                // URL Management
                ReadUrls, ManageAllUrls, ExportUrls,
                // Analytics
                ViewAnalytics, ViewDetailedAnalytics, ViewAllAnalytics, ExportAnalytics,
                // User Management
                ReadUsers, ViewUserActivity,
                // Profile
                UpdateOwnProfile, ChangeOwnPassword, ManageOwnApiKeys, ManageOwnSessions,
                // Security
                ViewSecurityLogs, BanUsers, ReviewSuspiciousActivity,
                // API
                UseApi
            }
        },
        {
            DefaultRoles.EnterpriseUser,
            new[]
            {
                // URL Management
                CreateUrls, ReadUrls, UpdateUrls, DeleteUrls, ManageOwnUrls, CreateCustomUrls, CreateBulkUrls, ExportUrls,
                // Analytics
                ViewAnalytics, ViewDetailedAnalytics, ViewOwnAnalytics, ExportAnalytics, ViewRealTimeAnalytics,
                // Profile
                UpdateOwnProfile, ChangeOwnPassword, ManageOwnApiKeys, ManageOwnSessions,
                // Domains
                CreateCustomDomains, ManageOwnDomains,
                // Webhooks
                CreateWebhooks, ManageOwnWebhooks, ViewWebhookLogs,
                // Billing
                ManageOwnSubscription, ViewBilling,
                // API
                UseApi, UseAdvancedApi
            }
        },
        {
            DefaultRoles.PremiumUser,
            new[]
            {
                // URL Management
                CreateUrls, ReadUrls, UpdateUrls, DeleteUrls, ManageOwnUrls, CreateCustomUrls, ExportUrls,
                // Analytics
                ViewAnalytics, ViewDetailedAnalytics, ViewOwnAnalytics, ExportAnalytics,
                // Profile
                UpdateOwnProfile, ChangeOwnPassword, ManageOwnApiKeys, ManageOwnSessions,
                // Webhooks
                CreateWebhooks, ManageOwnWebhooks,
                // Billing
                ManageOwnSubscription, ViewBilling,
                // API
                UseApi
            }
        },
        {
            DefaultRoles.User,
            new[]
            {
                // URL Management
                CreateUrls, ReadUrls, UpdateUrls, DeleteUrls, ManageOwnUrls,
                // Analytics
                ViewAnalytics, ViewOwnAnalytics,
                // Profile
                UpdateOwnProfile, ChangeOwnPassword, ManageOwnApiKeys, ManageOwnSessions,
                // Billing
                ManageOwnSubscription, ViewBilling
            }
        },
        {
            DefaultRoles.ApiUser,
            new[]
            {
                // URL Management
                CreateUrls, ReadUrls, UpdateUrls, DeleteUrls, ManageOwnUrls,
                // Analytics
                ViewAnalytics, ViewOwnAnalytics,
                // API
                UseApi, UseAdvancedApi
            }
        },
        {
            DefaultRoles.ReadOnlyUser,
            new[]
            {
                // URL Management
                ReadUrls,
                // Analytics
                ViewAnalytics, ViewOwnAnalytics,
                // Profile
                UpdateOwnProfile, ChangeOwnPassword
            }
        },
        {
            DefaultRoles.Guest,
            new[]
            {
                // URL Management
                ReadUrls
            }
        }
    };
}
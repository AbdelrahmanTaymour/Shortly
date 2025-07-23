namespace Shortly.Domain.Enums;

[Flags]
public enum enPermissions : long
{
    None = 0,

    // URL Management
    CreateUrl = 1L << 0,      // 1
    ReadUrl = 1L << 1,        // 2
    UpdateUrl = 1L << 2,      // 4
    DeleteUrl = 1L << 3,      // 8
    BulkCreateUrl = 1L << 4,  // 16
    BulkDeleteUrl = 1L << 5,  // 32
    ExportUrls = 1L << 6,     // 64

    // Analytics & Reporting
    ViewBasicAnalytics = 1L << 7,      // 128
    ViewDetailedAnalytics = 1L << 8,   // 256
    ViewRealtimeAnalytics = 1L << 9,   // 512
    ExportAnalytics = 1L << 10,        // 1024
    CreateCustomReports = 1L << 11,    // 2048

    // Link Customization
    UseCustomDomain = 1L << 12,         // 4096
    CreateCustomAlias = 1L << 13,       // 8192
    CreateBrandedLinks = 1L << 14,      // 16384
    GenerateQrCodes = 1L << 15,         // 32768
    SetPasswordProtection = 1L << 16,   // 65536
    SetLinkExpiration = 1L << 17,       // 131072

    // Team & Organization
    InviteTeamMembers = 1L << 18,    // 262144
    RemoveTeamMembers = 1L << 19,    // 524288
    ManageTeamRoles = 1L << 20,      // 1048576
    ManageOrganization = 1L << 21,   // 2097152
    ManageBilling = 1L << 22,        // 4194304

    // API Access
    ApiRead = 1L << 23,      // 8388608
    ApiWrite = 1L << 24,     // 16777216
    ApiAdmin = 1L << 25,     // 33554432

    // Advanced Features
    ManageCampaigns = 1L << 26,     // 67108864
    ConfigureWebhooks = 1L << 27,   // 134217728
    ManageIntegrations = 1L << 28,  // 268435456

    // System Administration
    SystemAdmin = 1L << 29,     // 536870912

    // Convenience combinations
    // URL Operations
    BasicUrlOperations = CreateUrl | ReadUrl | UpdateUrl | DeleteUrl,
    AdvancedUrlOperations = BulkCreateUrl | BulkDeleteUrl,
    FullUrlManagement = BasicUrlOperations | AdvancedUrlOperations | ExportUrls,
    
    // Analytics
    BasicAnalytics = ViewBasicAnalytics,
    DetailedAnalytics = ViewDetailedAnalytics | ViewRealtimeAnalytics,
    FullAnalytics = ViewBasicAnalytics | ViewDetailedAnalytics | ViewRealtimeAnalytics | ExportAnalytics | CreateCustomReports,

    // Customization
    CustomizationFeatures = UseCustomDomain | CreateCustomAlias | CreateBrandedLinks | GenerateQrCodes | SetPasswordProtection | SetLinkExpiration,

    // Team & Organization
    TeamManagement = InviteTeamMembers | RemoveTeamMembers | ManageTeamRoles,
    OrganizationManagement = ManageOrganization | ManageBilling,
    FullTeamAndOrg = TeamManagement | OrganizationManagement,

    // API
    ApiUser = ApiRead | ApiWrite,
    ApiFullAccess = ApiUser | ApiAdmin,

    // Advanced
    AdvancedFeatures = ManageCampaigns | ConfigureWebhooks | ManageIntegrations,

    // Superuser
    AllPermissions = -1
}
namespace Shortly.Domain.Enums;

// TODO: REVISIT THIS AFTER BUILDING ALL FEATURE

[Flags]
public enum enPermissions : long
{
    None = 0,

    // URL Management
    CreateUrl = 1L << 0, // 1
    ReadUrl = 1L << 1, // 2
    UpdateUrl = 1L << 2, // 4
    DeleteUrl = 1L << 3, // 8
    BulkCreateUrl = 1L << 4, // 16
    BulkDeleteUrl = 1L << 5, // 32
    ExportUrls = 1L << 6, // 64

    // Analytics & Reporting
    ViewBasicAnalytics = 1L << 7, // 128
    ViewDetailedAnalytics = 1L << 8, // 256
    ViewRealtimeAnalytics = 1L << 9, // 512
    ExportAnalytics = 1L << 10, // 1024
    CreateCustomReports = 1L << 11, // 2048

    // Link Customization
    UseCustomDomain = 1L << 12, // 4096
    CreateCustomAlias = 1L << 13, // 8192
    CreateBrandedLinks = 1L << 14, // 16384
    GenerateQrCodes = 1L << 15, // 32768
    SetPasswordProtection = 1L << 16, // 65536
    SetLinkExpiration = 1L << 17, // 131072

    // Team & Organization
    InviteTeamMembers = 1L << 18, // 262144
    RemoveTeamMembers = 1L << 19, // 524288
    ManageTeamRoles = 1L << 20, // 1048576
    ManageOrganization = 1L << 21, // 2097152
    ManageBilling = 1L << 22, // 4194304

    // API Access
    ApiRead = 1L << 23, // 8388608
    ApiWrite = 1L << 24, // 16777216
    ApiAdmin = 1L << 25, // 33554432

    // Advanced Features
    ManageCampaigns = 1L << 26, // 67108864
    ConfigureWebhooks = 1L << 27, // 134217728
    ManageIntegrations = 1L << 28, // 268435456

    // User Management - Self Operations (Every authenticated user should have these)
    ViewOwnProfile = 1L << 29, // 536870912
    UpdateOwnProfile = 1L << 30, // 1073741824
    DeleteOwnAccount = 1L << 31, // 2147483648
    ChangeOwnPassword = 1L << 32, // 4294967296
    ManageOwnTwoFactor = 1L << 33, // 8589934592
    ViewOwnUsageStats = 1L << 34, // 17179869184

    // User Management - Administrative Operations
    ViewAllUsers = 1L << 35, // 34359738368
    SearchUsers = 1L << 36, // 68719476736
    LockUserAccounts = 1L << 37, // 137438953472
    UnlockUserAccounts = 1L << 38, // 274877906944
    ActivateUsers = 1L << 39, // 549755813888
    DeactivateUsers = 1L << 40, // 1099511627776
    ViewUsersAnalytics = 1L << 41, // 2199023255552
    ManageUserRoles = 1L << 42, // 4398046511104
    ViewUserDetails = 1L << 43, // 8796093022208
    ResetUserPasswords = 1L << 44, // 17592186044416


    // System Administration
    AddUser = 1L << 45, // 35184372088832
    UpdateUser = 1L << 46,
    SoftDeleteUser = 1L << 47,
    HardDeleteUser = 1L << 48,
    SystemAdmin = 1L << 49,

    // Convenience combinations
    // URL Operations
    BasicUrlOperations = CreateUrl | ReadUrl | UpdateUrl | DeleteUrl,
    AdvancedUrlOperations = BulkCreateUrl | BulkDeleteUrl,
    FullUrlManagement = BasicUrlOperations | AdvancedUrlOperations | ExportUrls,

    // Analytics
    BasicAnalytics = ViewBasicAnalytics,
    DetailedAnalytics = ViewDetailedAnalytics | ViewRealtimeAnalytics,

    FullAnalytics = ViewBasicAnalytics | ViewDetailedAnalytics | ViewRealtimeAnalytics | ExportAnalytics |
                    CreateCustomReports,

    // Customization
    CustomizationFeatures = UseCustomDomain | CreateCustomAlias | CreateBrandedLinks | GenerateQrCodes |
                            SetPasswordProtection | SetLinkExpiration,

    // User Self-Management (Basic user permissions)
    SelfManagement = ViewOwnProfile | UpdateOwnProfile | DeleteOwnAccount | ChangeOwnPassword | ManageOwnTwoFactor |
                     ViewOwnUsageStats,

    // User Administration (Admin permissions for managing other users)
    UserAdministration = ViewAllUsers | SearchUsers | LockUserAccounts | UnlockUserAccounts | ActivateUsers |
                         DeactivateUsers | ViewUsersAnalytics | ViewUserDetails,

    // Advanced User Administration (Super admin permissions)
    AdvancedUserAdministration = UserAdministration | ManageUserRoles | ResetUserPasswords,

    // Team & Organization
    TeamManagement = InviteTeamMembers | RemoveTeamMembers | ManageTeamRoles,
    OrganizationManagement = ManageOrganization | ManageBilling,
    FullTeamAndOrg = TeamManagement | OrganizationManagement,

    // API
    ApiUser = ApiRead | ApiWrite,
    ApiFullAccess = ApiUser | ApiAdmin,

    // Advanced Features
    AdvancedFeatures = ManageCampaigns | ConfigureWebhooks | ManageIntegrations,

    // Superuser
    AllPermissions = ~None
}
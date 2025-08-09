namespace Shortly.Domain.Enums;

// TODO: REVISIT THIS AFTER BUILDING ALL FEATURE

[Flags]
public enum enPermissions : long
{
    None = 0,

    // URL Management
    CreateUrl               = 1L << 0,
    ReadUrl                 = 1L << 1,
    UpdateUrl               = 1L << 2,
    DeleteUrl               = 1L << 3,
    BulkCreateUrl           = 1L << 4,
    BulkDeleteUrl           = 1L << 5,
    ExportUrls              = 1L << 6,

    // Analytics & Reporting
    ViewBasicAnalytics      = 1L << 7,
    ViewDetailedAnalytics   = 1L << 8,
    ViewRealtimeAnalytics   = 1L << 9,
    ExportAnalytics         = 1L << 10,
    CreateCustomReports     = 1L << 11,

    // Link Customization
    CreateCustomAlias       = 1L << 12,
    CreateBrandedLinks      = 1L << 13,
    GenerateQrCodes         = 1L << 14,
    SetPasswordProtection   = 1L << 15,
    SetLinkExpiration       = 1L << 16,

    // Team & Organization
    InviteTeamMembers       = 1L << 17,
    RemoveTeamMembers       = 1L << 18,
    ManageTeamRoles         = 1L << 19,
    ManageOrganization      = 1L << 20,
    ManageBilling           = 1L << 21,

    // User Management - Self Operations
    ViewOwnProfile          = 1L << 22,
    UpdateOwnProfile        = 1L << 23,
    DeleteOwnAccount        = 1L << 24,
    ChangeOwnPassword       = 1L << 25,
    ManageOwnTwoFactor      = 1L << 26,
    ViewOwnUsageStats       = 1L << 27,

     // ===== User Management (Admin) =====
    ViewUsers               = 1L << 28,
    ViewUserDetails         = 1L << 29,
    ManageUserSecurity      = 1L << 30,
    ManageUserActivation    = 1L << 31,
    ManageUserAvailability  = 1L << 32,
    ViewUserUsage           = 1L << 33,
    ManageUserUsage         = 1L << 34,
    ManageUserRoles         = 1L << 35,
    ResetUserPasswords      = 1L << 36,

    // ===== System Administration =====
    AddUser                 = 1L << 37,
    UpdateUser              = 1L << 38,
    SoftDeleteUser          = 1L << 39,
    HardDeleteUser          = 1L << 40,

    // ===== Permission Groups =====
    // URL Operations
    BasicUrlOperations      = CreateUrl | ReadUrl | UpdateUrl | DeleteUrl,
    AdvancedUrlOperations   = BulkCreateUrl | BulkDeleteUrl,
    FullUrlManagement       = BasicUrlOperations | AdvancedUrlOperations | ExportUrls,

    // Analytics
    BasicAnalytics          = ViewBasicAnalytics,
    DetailedAnalytics       = ViewDetailedAnalytics | ViewRealtimeAnalytics,
    FullAnalytics           = ViewBasicAnalytics | ViewDetailedAnalytics |
                              ViewRealtimeAnalytics | ExportAnalytics | CreateCustomReports,

    // Customization
    CustomizationFeatures   = CreateCustomAlias | CreateBrandedLinks | GenerateQrCodes |
                              SetPasswordProtection | SetLinkExpiration,

    // Self-Management
    SelfManagement          = ViewOwnProfile | UpdateOwnProfile | DeleteOwnAccount |
                              ChangeOwnPassword | ManageOwnTwoFactor | ViewOwnUsageStats,

    // User Administration
    UserAdministration      = ViewUsers | ViewUserUsage | ViewUserDetails,
    AdvancedUserAdministration = UserAdministration | ManageUserRoles | ResetUserPasswords |
                                  ManageUserSecurity | ManageUserActivation | ManageUserAvailability | ManageUserUsage,

    // Team & Organization
    TeamManagement          = InviteTeamMembers | RemoveTeamMembers | ManageTeamRoles,
    OrganizationManagement  = ManageOrganization | ManageBilling,
    FullTeamAndOrg          = TeamManagement | OrganizationManagement,

    // Full System
    AllPermissions          = ~None
}
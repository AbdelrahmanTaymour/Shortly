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
    BulkUpdateUrl           = 1L << 6,
    ExportUrls              = 1L << 7,
    ReadOrganizationUrl     = 1L << 8,

    // Analytics & Reporting
    ReadAnalytics           = 1L << 9,
    ReadUserAnalytics       = 1L << 10,
    ReadOrgAnalytics        = 1L << 11,
    CreateCustomReports     = 1L << 12,

    // Link Customization
    CreateCustomAlias       = 1L << 13,
    CreateBrandedLinks      = 1L << 14,
    GenerateQrCodes         = 1L << 15,
    SetPasswordProtection   = 1L << 16,
    SetLinkExpiration       = 1L << 17,

    // Team & Organization
    InviteTeamMembers       = 1L << 18,
    RemoveTeamMembers       = 1L << 19,
    ManageTeamRoles         = 1L << 20,
    ManageOrganization      = 1L << 21,
    ManageBilling           = 1L << 22,

    // User Management - Self Operations
    ViewOwnProfile          = 1L << 23,
    UpdateOwnProfile        = 1L << 24,
    DeleteOwnAccount        = 1L << 25,
    ChangeOwnPassword       = 1L << 26,
    ManageOwnTwoFactor      = 1L << 27,
    ViewOwnUsageStats       = 1L << 28,

    // ===== User Management (Admin) =====
    ViewUsers               = 1L << 29,
    ViewUserDetails         = 1L << 30,
    ManageUserSecurity      = 1L << 31,
    ManageUserActivation    = 1L << 32,
    ManageUserAvailability  = 1L << 33,
    ViewUserUsage           = 1L << 34,
    ManageUserUsage         = 1L << 35,
    ManageUserRoles         = 1L << 36,
    ResetUserPasswords      = 1L << 37,

    // ===== System Administration =====
    AddUser                 = 1L << 38,
    UpdateUser              = 1L << 39,
    SoftDeleteUser          = 1L << 40,
    HardDeleteUser          = 1L << 41,

    // ===== Permission Groups =====
    BasicUrlOperations      = CreateUrl | ReadUrl | UpdateUrl | DeleteUrl,
    AdvancedUrlOperations   = BulkCreateUrl | BulkDeleteUrl | BulkUpdateUrl,
    FullUrlManagement       = BasicUrlOperations | AdvancedUrlOperations | ExportUrls,

    BasicAnalytics          = ReadAnalytics,
    DetailedAnalytics       = ReadUserAnalytics,
    FullAnalytics           = ReadAnalytics | ReadUserAnalytics | ReadOrgAnalytics | CreateCustomReports,

    CustomizationFeatures   = CreateCustomAlias | CreateBrandedLinks | GenerateQrCodes |
                              SetPasswordProtection | SetLinkExpiration,

    SelfManagement          = ViewOwnProfile | UpdateOwnProfile | DeleteOwnAccount |
                              ChangeOwnPassword | ManageOwnTwoFactor | ViewOwnUsageStats,

    UserAdministration      = ViewUsers | ViewUserUsage | ViewUserDetails,
    AdvancedUserAdministration = UserAdministration | ManageUserRoles | ResetUserPasswords |
                                  ManageUserSecurity | ManageUserActivation | ManageUserAvailability | ManageUserUsage,

    TeamManagement          = InviteTeamMembers | RemoveTeamMembers | ManageTeamRoles,
    OrganizationManagement  = ManageOrganization | ManageBilling,
    FullTeamAndOrg          = TeamManagement | OrganizationManagement,

    AllPermissions          = ~None
}
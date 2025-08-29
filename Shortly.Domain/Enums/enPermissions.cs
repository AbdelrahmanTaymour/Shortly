namespace Shortly.Domain.Enums;

// TODO: REVISIT THIS AFTER BUILDING ALL FEATURE
[Flags]
public enum enPermissions : long
{
    None = 0,

    // ===== LINK MANAGEMENT (0-15) =====
    // Basic CRUD Operations
    CreateLink              = 1L << 0,   // Create new links
    ReadOwnLinks            = 1L << 1,   // View own links
    UpdateOwnLinks          = 1L << 2,   // Edit own links
    DeleteOwnLinks          = 1L << 3,   // Delete own links
    
    // Organization Link Operations
    ReadOrgLinks            = 1L << 4,   // View organization links
    UpdateOrgLinks          = 1L << 5,   // Edit organization links
    DeleteOrgLinks          = 1L << 6,   // Delete organization links
    
    // Bulk Operations
    BulkCreateLinks         = 1L << 7,   // Bulk create links
    BulkUpdateLinks         = 1L << 8,   // Bulk update links
    BulkDeleteLinks         = 1L << 9,   // Bulk delete links
    
    // Link Features
    CreateCustomAlias       = 1L << 10,  // Create custom aliases
    CreateBrandedLinks      = 1L << 11,  // Create branded links
    SetLinkExpiration       = 1L << 12,  // Set expiration dates
    SetPasswordProtection   = 1L << 13,  // Password protects links
    GenerateQrCodes         = 1L << 14,  // Generate QR codes
    ExportLinks             = 1L << 15,  // Export link data

    // ===== ANALYTICS & REPORTING (16-23) =====
    ReadOwnAnalytics        = 1L << 16,  // View own link analytics
    ReadTeamAnalytics       = 1L << 17,  // View team analytics
    ReadOrgAnalytics        = 1L << 18,  // View organization analytics
    CreateCustomReports     = 1L << 19,  // Create custom reports
    ExportAnalytics         = 1L << 20,  // Export analytics data
    ViewRealTimeStats       = 1L << 21,  // View real-time statistics
    ViewHistoricalData      = 1L << 22,  // Access historical data
    ConfigureAnalytics      = 1L << 23,  // Configure analytics settings

    // ===== TEAM MANAGEMENT (24-31) =====
    ViewTeams               = 1L << 24,  // View teams
    CreateTeam              = 1L << 25,  // Create new teams
    UpdateTeam              = 1L << 26,  // Edit team details
    DeleteTeam              = 1L << 27,  // Delete teams
    ManageTeamMembers       = 1L << 28,  // Add/remove team members
    ViewTeamMembers         = 1L << 29,  // View team member list
    AssignTeamRoles         = 1L << 30,  // Assign roles within team
    TransferTeamOwnership   = 1L << 31,  // Transfer team ownership

    // ===== ORGANIZATION MANAGEMENT (32-38) =====
    ViewOrganization        = 1L << 32,  // View organization details
    UpdateOrganization      = 1L << 33,  // Edit organization settings
    DeleteOrganization      = 1L << 34,  // Delete organization
    ManageOrganization      = 1L << 35,  // Manage organization
    ManageOrgBilling        = 1L << 36,  // Handle billing/subscriptions
    ManageOrgMembers        = 1L << 37,  // Manage Organization Members
    TransferOrgOwnership    = 1L << 38,  // Transfer organization ownership

    // ===== USER INVITATIONS & MEMBERSHIP (39-43) =====
    ViewInvitations         = 1L << 39,  // View pending invitations
    CreateInvitations       = 1L << 40,  // Send invitations
    ManageInvitations       = 1L << 41,  // Manage invitations
    ViewMembers             = 1L << 42,  // View organization members
    RemoveMembers           = 1L << 43,  // Remove members from org

    // ===== SELF-MANAGEMENT (44-44) =====
    ManageOwnAccount        = 1L << 44,  // (View, Edit, Change password, View usage, and Delete) own profile

    // ===== USER ADMINISTRATION (45-56) =====
    ViewUsers               = 1L << 45,  // View all users (admin)
    ViewUserDetails         = 1L << 46,  // View detailed user info
    ViewUrlsAnalytics       = 1L << 47,  // View all ShortUrl analytics
    ReadUrls                = 1L << 48,  // View all ShortUrls
    CreateUser              = 1L << 49,  // Create new users
    UpdateUser              = 1L << 50,  // Edit user details
    DeactivateUser          = 1L << 51,  // Deactivate user account
    ReactivateUser          = 1L << 52,  // Reactivate user account
    ResetUserPassword       = 1L << 53,  // Reset user passwords
    ManageUserAccessibility = 1L << 54,  // Manage user security and accessibility
    ManageUserUsage         = 1L << 55,  // Manage user usage
    ManageOrgUsage          = 1L << 56,  // Manage organization usage

    // ===== ADVANCED USER ADMINISTRATION (57-59) =====
    ViewUserMemberships     = 1L << 57,  // View User Memberships
    DeleteUser              = 1L << 58,  // Permanently delete users
    CleanupSystemData       = 1L << 59,  // Cleanup System Data

    // ===== COMMON PERMISSION COMBINATIONS =====
    // Basic User Permissions
    BasicUser = CreateLink | ReadOwnLinks | UpdateOwnLinks | DeleteOwnLinks | 
                ReadOwnAnalytics | ManageOwnAccount,

    // Team Member Permissions
    TeamMember = BasicUser | ReadTeamAnalytics | ViewTeamMembers,

    // Team Manager Permissions
    TeamManager = TeamMember | ReadOrgLinks | CreateCustomAlias | CreateBrandedLinks |
                  SetLinkExpiration | SetPasswordProtection | GenerateQrCodes |
                  ManageTeamMembers | AssignTeamRoles,

    // Organization Admin Permissions
    OrgAdmin = TeamManager | ReadOrgAnalytics | CreateCustomReports | ExportAnalytics |
               ViewOrganization | UpdateOrganization | ManageOrganization |
               ViewInvitations | CreateInvitations | ManageInvitations |
               ViewMembers | RemoveMembers | ViewTeams | CreateTeam |
               UpdateTeam | DeleteTeam,

    // Organization Owner Permissions
    OrgOwner = OrgAdmin | DeleteOrganization | TransferOrgOwnership | 
               TransferTeamOwnership | ManageOrgBilling | ManageOrgMembers,

    // Super Admin Permissions
    SuperAdmin = OrgOwner | ViewUsers | ViewUserDetails | CreateUser | UpdateUser |
                 DeactivateUser | ReactivateUser | ResetUserPassword | ViewUserMemberships |
                 DeleteUser | ManageUserAccessibility | ViewUrlsAnalytics | ReadUrls | ManageUserUsage | ManageOrgUsage,

    // System Administrator (Full Access)
    SystemAdmin = ~None // All permissions
}
using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts.Configurations;

namespace Shortly.Infrastructure.DbContexts;

/// <summary>
///     Represents the Entity Framework Core DbContext for managing Shortly application database operations.
///     Provides DbSet properties for entity mapping and implements custom model configurations.
/// </summary>
public class SqlServerDbContext(DbContextOptions<SqlServerDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
    private static readonly Guid AdminId = new("d27b9c92-747d-4b9d-bc65-083656729e24");
    private static readonly DateTime AdminCreatedAt = new(2025, 12, 12, 11, 32, 20, DateTimeKind.Utc);

    public DbSet<ShortUrl> ShortUrls { get; set; }
    public DbSet<ClickEvent> ClickEvents { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<UserSecurity> UserSecurity { get; set; }
    public DbSet<UserUsage> UserUsage { get; set; }
    public DbSet<UserActionToken> UserActionTokens { get; set; }
    public DbSet<UserAuditLog> UserAuditLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationUsage> OrganizationUsage { get; set; }
    public DbSet<OrganizationTeam> OrganizationTeams { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<OrganizationTeamMember> OrganizationTeamMembers { get; set; }
    public DbSet<OrganizationInvitation> OrganizationInvitations { get; set; }
    public DbSet<OrganizationAuditLog> OrganizationAuditLogs { get; set; }
    public DbSet<EmailChangeToken> EmailChangeTokens { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Role> Roles { get; set; }

    /// <summary>
    ///     Configures the model for the ShortlyDbContext by applying entity configurations
    ///     and seeding reference and admin data.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
        modelBuilder.ApplyConfiguration(new UserSecurityConfiguration());
        modelBuilder.ApplyConfiguration(new UserUsageConfiguration());
        modelBuilder.ApplyConfiguration(new UserActionTokenConfiguration());
        modelBuilder.ApplyConfiguration(new UserAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new ShortUrlConfiguration());
        modelBuilder.ApplyConfiguration(new ClickEventConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationUsageConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationTeamConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationMemberConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationTeamMemberConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationInvitationConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new EmailChangeTokenConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionPlanConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());

        // -----------------------------------------------------------------
        // Reference data — SubscriptionPlans
        // -----------------------------------------------------------------
        modelBuilder.Entity<SubscriptionPlan>().HasData(
            new SubscriptionPlan
            {
                Id = enSubscriptionPlan.Free,
                Name = "Free",
                Description = "Try it out for free",
                Price = 0,
                MaxQrCodesPerMonth = 2,
                MaxLinksPerMonth = 5,
                ClickDataRetentionDays = 0,
                LinkAnalysis = false,
                BulkCreation = false,
                LinkProtection = false,
                CustomShortCode = false,
                CampaignTracking = false,
                GeoDeviceTracking = false
            },
            new SubscriptionPlan
            {
                Id = enSubscriptionPlan.Starter,
                Name = "Starter",
                Description = "Unlock powerful data",
                Price = 10,
                MaxQrCodesPerMonth = 5,
                MaxLinksPerMonth = 100,
                ClickDataRetentionDays = 30,
                LinkAnalysis = true,
                CustomShortCode = true,
                BulkCreation = false,
                LinkProtection = false,
                CampaignTracking = false,
                GeoDeviceTracking = false
            },
            new SubscriptionPlan
            {
                Id = enSubscriptionPlan.Professional,
                Name = "Professional",
                Description = "Create memorable brand experiences",
                Price = 50,
                MaxQrCodesPerMonth = 10,
                MaxLinksPerMonth = 500,
                ClickDataRetentionDays = 120,
                LinkAnalysis = true,
                CustomShortCode = true,
                BulkCreation = true,
                LinkProtection = true,
                CampaignTracking = false,
                GeoDeviceTracking = false
            },
            new SubscriptionPlan
            {
                Id = enSubscriptionPlan.Enterprise,
                Name = "Enterprise",
                Description = "Track your brand in depth",
                Price = 200,
                MaxQrCodesPerMonth = 200,
                MaxLinksPerMonth = 3000,
                ClickDataRetentionDays = 365,
                LinkAnalysis = true,
                CustomShortCode = true,
                BulkCreation = true,
                LinkProtection = true,
                CampaignTracking = true,
                GeoDeviceTracking = true
            }
        );

        // -----------------------------------------------------------------
        // Reference data — Roles
        // -----------------------------------------------------------------
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = enUserRole.Viewer,
                RoleName = "Viewer",
                Description = "Read-only access to resources.",
                DefaultPermissions = (long)(
                    enPermissions.ReadOwnAnalytics |
                    enPermissions.ReadTeamAnalytics |
                    enPermissions.ReadOrgAnalytics)
            },
            new Role
            {
                Id = enUserRole.Member,
                RoleName = "Member",
                Description = "Can manage and track their own content.",
                DefaultPermissions = (long)enPermissions.TeamMember
            },
            new Role
            {
                Id = enUserRole.TeamManager,
                RoleName = "TeamManager",
                Description = "Can manage their team and content.",
                DefaultPermissions = (long)enPermissions.TeamManager
            },
            new Role
            {
                Id = enUserRole.OrgAdmin,
                RoleName = "OrgAdmin",
                Description = "Can manage users and settings for the organization.",
                DefaultPermissions = (long)enPermissions.OrgAdmin
            },
            new Role
            {
                Id = enUserRole.OrgOwner,
                RoleName = "OrgOwner",
                Description = "Owns the organization with full control.",
                DefaultPermissions = (long)enPermissions.OrgOwner
            },
            new Role
            {
                Id = enUserRole.SuperAdmin,
                RoleName = "PlatformAdmin",
                Description = "Platform-wide admin access.",
                DefaultPermissions = (long)enPermissions.SuperAdmin
            },
            new Role
            {
                Id = enUserRole.SystemAdmin,
                RoleName = "SystemAdmin",
                Description = "System-wide root access.",
                DefaultPermissions = (long)enPermissions.SystemAdmin
            }
        );

        // -----------------------------------------------------------------
        // Admin user seed
        // -----------------------------------------------------------------
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = AdminId,
            Email = "taymour@gmail.com",
            Username = "taymour",
            PasswordHash = "$2a$10$0EjZyXu5orwyTJ2GZyVRWeRSL0bnR.FrPx0aFi5GORCoDWLVNR6Ca",
            SubscriptionPlanId = enSubscriptionPlan.Professional,
            Permissions = -1L, // SystemAdmin — all bits set
            IsActive = true,
            IsEmailConfirmed = true,
            IsOAuthUser = false,
            IsDeleted = false,
            CreatedAt = AdminCreatedAt,
            UpdatedAt = AdminCreatedAt
        });

        modelBuilder.Entity<UserProfile>().HasData(new UserProfile
        {
            UserId = AdminId,
            Name = "Abdelrahman Taymour",
            UpdatedAt = AdminCreatedAt
        });

        modelBuilder.Entity<UserSecurity>().HasData(new UserSecurity
        {
            UserId = AdminId,
            UpdatedAt = AdminCreatedAt
        });

        modelBuilder.Entity<UserUsage>().HasData(new UserUsage
        {
            UserId = AdminId,
            MonthlyResetDate = AdminCreatedAt.AddMonths(1)
        });
    }
}
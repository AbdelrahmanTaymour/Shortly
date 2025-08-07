using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts.Configurations;

namespace Shortly.Infrastructure.DbContexts;

/// <summary>
/// Represents the Entity Framework Core DbContext for managing Shortly application database operations.
/// Provides DbSet properties for entity mapping and implements custom model configurations.
/// </summary>
public class SQLServerDbContext : DbContext
{
    public SQLServerDbContext(DbContextOptions<SQLServerDbContext> dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<ShortUrl> ShortUrls { get; set; }
    public DbSet<ClickEvent> ClickEvents { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<UserSecurity> UserSecurity { get; set; }
    public DbSet<UserUsage> UserUsage { get; set; }
    public DbSet<UserAuditLog> UserAuditLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationTeam> OrganizationTeams { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<OrganizationTeamMember> OrganizationTeamMembers { get; set; }
    public DbSet<OrganizationInvitation> OrganizationInvitations { get; set; }
    public DbSet<OrganizationAuditLog> OrganizationAuditLogs { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Role> Roles { get; set; }

    /// <summary>
    /// Configures the model for the ShortlyDbContext by applying entity configurations and other custom settings.
    /// </summary>
    /// <param name="modelBuilder">An instance of <see cref="ModelBuilder"/> used to configure the model and its entities.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
        modelBuilder.ApplyConfiguration(new UserSecurityConfiguration());
        modelBuilder.ApplyConfiguration(new UserUsageConfiguration());
        modelBuilder.ApplyConfiguration(new UserAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new ShortUrlConfiguration());
        modelBuilder.ApplyConfiguration(new ClickEventConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationTeamConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationMemberConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationTeamMemberConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationInvitationConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionPlanConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());


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

        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = (byte)enUserRole.Viewer,
                RoleName = "Viewer",
                Description = "Read-only access to resources.",
                DefaultPermissions = (long)(
                    enPermissions.ViewBasicAnalytics |
                    enPermissions.ViewOwnProfile |
                    enPermissions.ViewOwnUsageStats)
            },
            new Role
            {
                Id = (byte)enUserRole.Member,
                RoleName = "Member",
                Description = "Can manage and track their own content.",
                DefaultPermissions = (long)(
                    enPermissions.FullUrlManagement |
                    enPermissions.GenerateQrCodes |
                    enPermissions.BasicAnalytics |
                    enPermissions.SelfManagement)
            },
            new Role
            {
                Id = (byte)enUserRole.TeamManager,
                RoleName = "TeamManager",
                Description = "Can manage their team and content.",
                DefaultPermissions = (long)(
                    enPermissions.FullUrlManagement |
                    enPermissions.CustomizationFeatures |
                    enPermissions.FullAnalytics |
                    enPermissions.TeamManagement |
                    enPermissions.SelfManagement)
            },
            new Role
            {
                Id = (byte)enUserRole.OrgAdmin,
                RoleName = "OrgAdmin",
                Description = "Can manage users and settings for the organization.",
                DefaultPermissions = (long)(
                    enPermissions.FullUrlManagement |
                    enPermissions.FullAnalytics |
                    enPermissions.CustomizationFeatures |
                    enPermissions.FullTeamAndOrg |
                    enPermissions.SelfManagement |
                    enPermissions.UserAdministration)
            },
            new Role
            {
                Id = (byte)enUserRole.OrgOwner,
                RoleName = "OrgOwner",
                Description = "Owns the organization with full control.",
                DefaultPermissions = (long)(enPermissions.AllPermissions & ~enPermissions.SystemAdmin)
            },
            new Role
            {
                Id = (byte)enUserRole.Admin,
                RoleName = "PlatformAdmin",
                Description = "Platform-wide admin access.",
                DefaultPermissions = (long)(enPermissions.AllPermissions & ~enPermissions.SystemAdmin)
            },
            new Role
            {
                Id = (byte)enUserRole.SuperAdmin,
                RoleName = "SuperAdmin",
                Description = "System-wide root access.",
                DefaultPermissions = (long)enPermissions.AllPermissions
            }
        );
    }
}
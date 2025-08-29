using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    RoleName = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultPermissions = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MaxQrCodesPerMonth = table.Column<int>(type: "int", nullable: false),
                    MaxLinksPerMonth = table.Column<int>(type: "int", nullable: false),
                    ClickDataRetentionDays = table.Column<int>(type: "int", nullable: false),
                    LinkAnalysis = table.Column<bool>(type: "bit", nullable: false),
                    BulkCreation = table.Column<bool>(type: "bit", nullable: false),
                    LinkProtection = table.Column<bool>(type: "bit", nullable: false),
                    CustomShortCode = table.Column<bool>(type: "bit", nullable: false),
                    CampaignTracking = table.Column<bool>(type: "bit", nullable: false),
                    GeoDeviceTracking = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(320)", unicode: false, maxLength: 320, nullable: false),
                    Username = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    SubscriptionPlanId = table.Column<byte>(type: "tinyint", nullable: false),
                    Permissions = table.Column<long>(type: "bigint", nullable: false, defaultValue: 15L),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsEmailConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MemberLimit = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsSubscribed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProfilePictureUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSecurity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TwoFactorSecret = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecurity", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserSecurity_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserUsage",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonthlyLinksCreated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MonthlyQrCodesCreated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalLinksCreated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalQrCodesCreated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MonthlyResetDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(month, 1, GETUTCDATE())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUsage", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserUsage_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)2),
                    CustomPermissions = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    InvitedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Event = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    TargetEntity = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    TargetId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationAuditLogs_OrganizationMembers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationAuditLogs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrganizationInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedUserEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    InvitedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitationToken = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true, defaultValueSql: "DATEADD(day, 5, GETUTCDATE())"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsExpired = table.Column<bool>(type: "bit", nullable: false, computedColumnSql: "CASE WHEN [ExpiresAt] < GETUTCDATE() THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationInvitations_OrganizationMembers_InvitedBy",
                        column: x => x.InvitedBy,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationInvitations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationTeams_OrganizationMembers_TeamManagerId",
                        column: x => x.TeamManagerId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationTeams_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShortUrls",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ShortCode = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    OwnerType = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnonymousSessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AnonymousIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TrackingEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ClickLimit = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    TotalClicks = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsPasswordProtected = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortUrls", x => x.Id);
                    table.CheckConstraint("CK_ShortUrls_CreatedByMember", "(\n            -- If OrganizationId is set, CreatedByMemberId should be set\n            ([OrganizationId] IS NOT NULL AND [CreatedByMemberId] IS NOT NULL)\n            OR\n            -- If OrganizationId is null, CreatedByMemberId should be null\n            ([OrganizationId] IS NULL AND [CreatedByMemberId] IS NULL)\n        )");
                    table.CheckConstraint("CK_ShortUrls_SingleOwner", "(\n            -- User owned: OwnerType = 1, UserId is set, OrganizationId is null, no anonymous fields\n            ([OwnerType] = 1 AND [UserId] IS NOT NULL AND [OrganizationId] IS NULL AND [AnonymousSessionId] IS NULL AND [IpAddress] IS NULL)\n            OR\n            -- Organization owned: OwnerType = 2, OrganizationId is set, UserId is null, no anonymous fields  \n            ([OwnerType] = 2 AND [OrganizationId] IS NOT NULL AND [UserId] IS NULL AND [AnonymousSessionId] IS NULL AND [IpAddress] IS NULL)\n            OR\n            -- Anonymous owned: OwnerType = 3, Both UserId and OrganizationId are null, anonymous fields can be set\n            ([OwnerType] = 3 AND [UserId] IS NULL AND [OrganizationId] IS NULL)\n        )");
                    table.ForeignKey(
                        name: "FK_ShortUrls_OrganizationMembers_CreatedByMemberId",
                        column: x => x.CreatedByMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShortUrls_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShortUrls_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationTeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationTeamMembers_OrganizationMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationTeamMembers_OrganizationTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "OrganizationTeams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClickEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShortUrlId = table.Column<long>(type: "bigint", nullable: false),
                    ClickedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: false),
                    SessionId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Browser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OperatingSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Device = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Referrer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferrerDomain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrafficSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UtmSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UtmMedium = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UtmCampaign = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UtmTerm = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UtmContent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClickEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClickEvents_ShortUrls_ShortUrlId",
                        column: x => x.ShortUrlId,
                        principalTable: "ShortUrls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "DefaultPermissions", "Description", "RoleName" },
                values: new object[,]
                {
                    { (byte)1, 17716740224L, "Read-only access to resources.", "Viewer" },
                    { (byte)2, 33822900479L, "Can manage and track their own content.", "Member" },
                    { (byte)3, 33824964607L, "Can manage their team and content.", "TeamManager" },
                    { (byte)4, 13193611051007L, "Can manage users and settings for the organization.", "OrgAdmin" },
                    { (byte)5, -562949953421313L, "Owns the organization with full control.", "OrgOwner" },
                    { (byte)6, -562949953421313L, "Platform-wide admin access.", "PlatformAdmin" },
                    { (byte)7, -1L, "System-wide root access.", "SystemAdmin" }
                });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "BulkCreation", "CampaignTracking", "ClickDataRetentionDays", "CustomShortCode", "Description", "GeoDeviceTracking", "LinkAnalysis", "LinkProtection", "MaxLinksPerMonth", "MaxQrCodesPerMonth", "Name", "Price" },
                values: new object[,]
                {
                    { (byte)1, false, false, 0, false, "Try it out for free", false, false, false, 5, 2, "Free", 0m },
                    { (byte)2, false, false, 30, true, "Unlock powerful data", false, true, false, 100, 5, "Starter", 10m },
                    { (byte)3, true, false, 120, true, "Create memorable brand experiences", false, true, true, 500, 10, "Professional", 50m },
                    { (byte)4, true, true, 365, true, "Track your brand in depth", true, true, true, 3000, 200, "Enterprise", 200m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShortUrlId",
                table: "ClickEvents",
                column: "ShortUrlId");

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShortUrlId_ClickedAt",
                table: "ClickEvents",
                columns: new[] { "ShortUrlId", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShortUrlId_Country",
                table: "ClickEvents",
                columns: new[] { "ShortUrlId", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShortUrlId_DeviceType",
                table: "ClickEvents",
                columns: new[] { "ShortUrlId", "DeviceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShortUrlId_TrafficSource",
                table: "ClickEvents",
                columns: new[] { "ShortUrlId", "TrafficSource" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAuditLogs_ActorId",
                table: "OrganizationAuditLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAuditLogs_OrganizationId",
                table: "OrganizationAuditLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAuditLogs_OrganizationId_TimeStamp",
                table: "OrganizationAuditLogs",
                columns: new[] { "OrganizationId", "TimeStamp" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_InvitationToken",
                table: "OrganizationInvitations",
                column: "InvitationToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_InvitedBy",
                table: "OrganizationInvitations",
                column: "InvitedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_OrganizationId_InvitedUserEmail_Status",
                table: "OrganizationInvitations",
                columns: new[] { "OrganizationId", "InvitedUserEmail", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_OrganizationId_IsActive",
                table: "OrganizationMembers",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_OrganizationId_UserId",
                table: "OrganizationMembers",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_RoleId",
                table: "OrganizationMembers",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_UserId",
                table: "OrganizationMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_IsActive",
                table: "Organizations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_IsActive_DeletedAt",
                table: "Organizations",
                columns: new[] { "IsActive", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OwnerId",
                table: "Organizations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationTeamMembers_MemberId",
                table: "OrganizationTeamMembers",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationTeamMembers_TeamId_MemberId",
                table: "OrganizationTeamMembers",
                columns: new[] { "TeamId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationTeams_OrganizationId_Name",
                table: "OrganizationTeams",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationTeams_TeamManagerId",
                table: "OrganizationTeams",
                column: "TeamManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_CreatedByMemberId",
                table: "ShortUrls",
                column: "CreatedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_IsActive_ExpiresAt",
                table: "ShortUrls",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_OrganizationId",
                table: "ShortUrls",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_OwnerType_AnonymousSessionId",
                table: "ShortUrls",
                columns: new[] { "OwnerType", "AnonymousSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_ShortCode",
                table: "ShortUrls",
                column: "ShortCode",
                unique: true,
                filter: "[ShortCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_UserId",
                table: "ShortUrls",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLogs_TimeStamp",
                table: "UserAuditLogs",
                column: "TimeStamp");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLogs_UserId",
                table: "UserAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLogs_UserId_TimeStamp",
                table: "UserAuditLogs",
                columns: new[] { "UserId", "TimeStamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted",
                table: "Users",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted_IsActive",
                table: "Users",
                columns: new[] { "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_SubscriptionPlanId",
                table: "Users",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurity_PasswordResetToken",
                table: "UserSecurity",
                column: "PasswordResetToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClickEvents");

            migrationBuilder.DropTable(
                name: "OrganizationAuditLogs");

            migrationBuilder.DropTable(
                name: "OrganizationInvitations");

            migrationBuilder.DropTable(
                name: "OrganizationTeamMembers");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserAuditLogs");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserSecurity");

            migrationBuilder.DropTable(
                name: "UserUsage");

            migrationBuilder.DropTable(
                name: "ShortUrls");

            migrationBuilder.DropTable(
                name: "OrganizationTeams");

            migrationBuilder.DropTable(
                name: "OrganizationMembers");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");
        }
    }
}

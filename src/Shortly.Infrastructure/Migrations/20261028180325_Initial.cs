using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "subscription_plans",
                columns: table => new
                {
                    id = table.Column<byte>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    max_qr_codes_per_month = table.Column<int>(type: "integer", nullable: false),
                    max_links_per_month = table.Column<int>(type: "integer", nullable: false),
                    click_data_retention_days = table.Column<int>(type: "integer", nullable: false),
                    link_analysis = table.Column<bool>(type: "boolean", nullable: false),
                    bulk_creation = table.Column<bool>(type: "boolean", nullable: false),
                    link_protection = table.Column<bool>(type: "boolean", nullable: false),
                    custom_short_code = table.Column<bool>(type: "boolean", nullable: false),
                    campaign_tracking = table.Column<bool>(type: "boolean", nullable: false),
                    geo_device_tracking = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", unicode: false, maxLength: 320, nullable: false),
                    username = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", unicode: false, maxLength: 256, nullable: false),
                    role = table.Column<byte>(type: "smallint", nullable: false),
                    subscription_plan_id = table.Column<byte>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_email_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    google_id = table.Column<string>(type: "text", nullable: true),
                    google_profile_picture = table.Column<string>(type: "text", nullable: true),
                    is_o_auth_user = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_subscription_plans",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_users_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", unicode: false, maxLength: 500, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    member_limit = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_subscribed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_plan_id = table.Column<byte>(type: "smallint", nullable: false),
                    owner_id1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                    table.ForeignKey(
                        name: "fk_organizations_subscription_plans",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organizations_user_owner_id1",
                        column: x => x.owner_id1,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_organizations_users",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_action_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_type = table.Column<byte>(type: "smallint", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    new_email = table.Column<string>(type: "text", nullable: true),
                    user_id1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_action_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_action_tokens_user_user_id1",
                        column: x => x.user_id1,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_action_tokens_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ip_address = table.Column<string>(type: "character varying(45)", unicode: false, maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    user_id1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_audit_logs_user_user_id1",
                        column: x => x.user_id1,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_audit_logs_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profile",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    profile_picture_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    time_zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profile", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_profile_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_security",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lockout_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    two_factor_secret = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_security", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_security_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_usage",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monthly_links_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    monthly_qr_codes_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_links_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_qr_codes_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    monthly_reset_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP + INTERVAL '1 month'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_usage", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_usage_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)2),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    organization_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_members_organizations",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_members_organizations_organization_id1",
                        column: x => x.organization_id1,
                        principalTable: "organizations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_organization_members_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_members_users_user_id1",
                        column: x => x.user_id1,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_usage",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monthly_links_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    monthly_qr_codes_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_links_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_qr_codes_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    monthly_reset_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP + INTERVAL '1 month'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_usage", x => x.organization_id);
                    table.ForeignKey(
                        name: "fk_organization_usage_organizations",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_organizations",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    @event = table.Column<string>(name: "event", type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    target_entity = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    target_id = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    organization_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_id1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_audit_logs_organization_member_actor_id1",
                        column: x => x.actor_id1,
                        principalTable: "organization_members",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_organization_audit_logs_organization_members",
                        column: x => x.actor_id,
                        principalTable: "organization_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_audit_logs_organization_organization_id1",
                        column: x => x.organization_id1,
                        principalTable: "organizations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_organization_audit_logs_organizations",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_user_email = table.Column<string>(type: "character varying(320)", unicode: false, maxLength: 320, nullable: false),
                    invited_user_role_id = table.Column<byte>(type: "smallint", nullable: false),
                    invited_user_permissions = table.Column<long>(type: "bigint", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)1),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW() + INTERVAL '5 days'"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    organization_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    invited_user_role = table.Column<byte>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_invitations_organization_members",
                        column: x => x.invited_by,
                        principalTable: "organization_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_invitations_organizations",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_invitations_organizations_organization_id1",
                        column: x => x.organization_id1,
                        principalTable: "organizations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "organization_teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_manager_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_teams", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_teams_organization_members",
                        column: x => x.team_manager_id,
                        principalTable: "organization_members",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_organization_teams_organizations",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "short_urls",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    short_code = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    owner_type = table.Column<byte>(type: "smallint", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    anonymous_session_id = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", unicode: false, maxLength: 45, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tracking_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    click_limit = table.Column<int>(type: "integer", nullable: false),
                    total_clicks = table.Column<int>(type: "integer", nullable: false),
                    is_password_protected = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", unicode: false, maxLength: 256, nullable: true),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    organization_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_short_urls", x => x.id);
                    table.CheckConstraint("ck_short_urls_single_owner", "(owner_type = 1 AND user_id IS NOT NULL AND organization_id IS NULL AND created_by_member_id IS NULL AND anonymous_session_id IS NULL)\nOR (owner_type = 2 AND user_id IS NULL AND organization_id IS NOT NULL AND created_by_member_id IS NOT NULL AND anonymous_session_id IS NULL)\nOR (owner_type = 3 AND user_id IS NULL AND organization_id IS NULL AND created_by_member_id IS NULL AND anonymous_session_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_short_urls_organization_members",
                        column: x => x.created_by_member_id,
                        principalTable: "organization_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_short_urls_organization_members_organization_member_id",
                        column: x => x.organization_member_id,
                        principalTable: "organization_members",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_short_urls_organizations",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_short_urls_organizations_organization_id1",
                        column: x => x.organization_id1,
                        principalTable: "organizations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_short_urls_user_user_id1",
                        column: x => x.user_id1,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_short_urls_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    member_id1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_team_members_organization_members",
                        column: x => x.member_id,
                        principalTable: "organization_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_team_members_organization_members_member_id1",
                        column: x => x.member_id1,
                        principalTable: "organization_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_team_members_organization_teams",
                        column: x => x.team_id,
                        principalTable: "organization_teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "click_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    short_url_id = table.Column<long>(type: "bigint", nullable: false),
                    clicked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ip_address = table.Column<string>(type: "character varying(45)", unicode: false, maxLength: 45, nullable: false),
                    session_id = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    referrer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    referrer_domain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    traffic_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    utm_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    utm_medium = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    utm_campaign = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    utm_term = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    utm_content = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    browser = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    operating_system = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    device = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    device_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_click_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_click_events_short_urls",
                        column: x => x.short_url_id,
                        principalTable: "short_urls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "id", "bulk_creation", "campaign_tracking", "click_data_retention_days", "custom_short_code", "description", "geo_device_tracking", "link_analysis", "link_protection", "max_links_per_month", "max_qr_codes_per_month", "name", "price" },
                values: new object[,]
                {
                    { (byte)1, false, false, 0, false, "Try it out for free", false, false, false, 5, 2, "Free", 0m },
                    { (byte)2, false, false, 30, true, "Unlock powerful data", false, true, false, 100, 5, "Starter", 10m },
                    { (byte)3, true, true, 90, true, "For growing teams", false, true, true, 500, 10, "Professional", 50m },
                    { (byte)4, true, true, -1, true, "Unlimited everything", true, true, true, -1, -1, "Enterprise", 200m }
                });

            migrationBuilder.CreateIndex(
                name: "ix_click_events_short_url_id_clicked_at",
                table: "click_events",
                columns: new[] { "short_url_id", "clicked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_audit_logs_actor_id",
                table: "organization_audit_logs",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_audit_logs_actor_id1",
                table: "organization_audit_logs",
                column: "actor_id1");

            migrationBuilder.CreateIndex(
                name: "ix_organization_audit_logs_organization_id_timestamp",
                table: "organization_audit_logs",
                columns: new[] { "organization_id", "time_stamp" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_audit_logs_organization_id1",
                table: "organization_audit_logs",
                column: "organization_id1");

            migrationBuilder.CreateIndex(
                name: "ix_organization_invitations_invited_by",
                table: "organization_invitations",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "ix_organization_invitations_organization_id_email_status",
                table: "organization_invitations",
                columns: new[] { "organization_id", "invited_user_email", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_invitations_organization_id1",
                table: "organization_invitations",
                column: "organization_id1");

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_organization_id_is_active",
                table: "organization_members",
                columns: new[] { "organization_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_organization_id1",
                table: "organization_members",
                column: "organization_id1");

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_user_id",
                table: "organization_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_user_id1",
                table: "organization_members",
                column: "user_id1");

            migrationBuilder.CreateIndex(
                name: "ux_organization_members_organization_id_user_id",
                table: "organization_members",
                columns: new[] { "organization_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_teams_team_manager_id",
                table: "organization_teams",
                column: "team_manager_id");

            migrationBuilder.CreateIndex(
                name: "ux_organization_teams_organization_id_name",
                table: "organization_teams",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizations_is_active",
                table: "organizations",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_is_active_deleted_at",
                table: "organizations",
                columns: new[] { "is_active", "deleted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_organizations_owner_id1",
                table: "organizations",
                column: "owner_id1");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_subscription_plan_id",
                table: "organizations",
                column: "subscription_plan_id");

            migrationBuilder.CreateIndex(
                name: "ux_organizations_owner_id_name",
                table: "organizations",
                columns: new[] { "owner_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_organization_id",
                table: "refresh_tokens",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id_is_revoked_expires_at",
                table: "refresh_tokens",
                columns: new[] { "user_id", "is_revoked", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_anonymous_session_id",
                table: "short_urls",
                column: "anonymous_session_id",
                filter: "anonymous_session_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_created_by_member_id",
                table: "short_urls",
                column: "created_by_member_id",
                filter: "created_by_member_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_expires_at",
                table: "short_urls",
                column: "expires_at",
                filter: "expires_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_organization_id_created_at",
                table: "short_urls",
                columns: new[] { "organization_id", "created_at_utc" },
                descending: new[] { false, true },
                filter: "organization_id IS NOT NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "short_code", "original_url", "total_clicks", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_organization_id1",
                table: "short_urls",
                column: "organization_id1");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_organization_member_id",
                table: "short_urls",
                column: "organization_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_user_id_created_at",
                table: "short_urls",
                columns: new[] { "user_id", "created_at_utc" },
                descending: new[] { false, true },
                filter: "user_id IS NOT NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "short_code", "original_url", "total_clicks", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_user_id1",
                table: "short_urls",
                column: "user_id1");

            migrationBuilder.CreateIndex(
                name: "ux_short_urls_short_code",
                table: "short_urls",
                column: "short_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_subscription_plans_name",
                table: "subscription_plans",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_team_members_member_id",
                table: "team_members",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_team_members_member_id1",
                table: "team_members",
                column: "member_id1");

            migrationBuilder.CreateIndex(
                name: "ux_team_members_team_id_member_id",
                table: "team_members",
                columns: new[] { "team_id", "member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_action_tokens_user_id_token_type_used",
                table: "user_action_tokens",
                columns: new[] { "user_id", "token_type", "used" });

            migrationBuilder.CreateIndex(
                name: "ix_user_action_tokens_user_id1",
                table: "user_action_tokens",
                column: "user_id1");

            migrationBuilder.CreateIndex(
                name: "ux_user_action_tokens_token_hash",
                table: "user_action_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_audit_logs_timestamp",
                table: "user_audit_logs",
                column: "time_stamp");

            migrationBuilder.CreateIndex(
                name: "ix_user_audit_logs_user_id_timestamp",
                table: "user_audit_logs",
                columns: new[] { "user_id", "time_stamp" });

            migrationBuilder.CreateIndex(
                name: "ix_user_audit_logs_user_id1",
                table: "user_audit_logs",
                column: "user_id1");

            migrationBuilder.CreateIndex(
                name: "ix_users_deleted_by",
                table: "users",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_users_is_deleted_is_active",
                table: "users",
                columns: new[] { "is_deleted", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_users_subscription_plan_id",
                table: "users",
                column: "subscription_plan_id");

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "click_events");

            migrationBuilder.DropTable(
                name: "organization_audit_logs");

            migrationBuilder.DropTable(
                name: "organization_invitations");

            migrationBuilder.DropTable(
                name: "organization_usage");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "team_members");

            migrationBuilder.DropTable(
                name: "user_action_tokens");

            migrationBuilder.DropTable(
                name: "user_audit_logs");

            migrationBuilder.DropTable(
                name: "user_profile");

            migrationBuilder.DropTable(
                name: "user_security");

            migrationBuilder.DropTable(
                name: "user_usage");

            migrationBuilder.DropTable(
                name: "short_urls");

            migrationBuilder.DropTable(
                name: "organization_teams");

            migrationBuilder.DropTable(
                name: "organization_members");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "subscription_plans");
        }
    }
}

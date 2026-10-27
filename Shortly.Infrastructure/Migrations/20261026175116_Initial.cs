using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscription_plan",
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
                    table.PrimaryKey("pk_subscription_plan", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
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
                    table.PrimaryKey("pk_user", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_subscription_plan_subscription_plan_id",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "email_change_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    old_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    new_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_change_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_change_token_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization",
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
                    subscription_plan_id = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_subscription_plan_subscription_plan_id",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_user_owner_id",
                        column: x => x.owner_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_token_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_action_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_type = table.Column<byte>(type: "smallint", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_action_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_action_token_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ip_address = table.Column<string>(type: "character varying(45)", unicode: false, maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_audit_log_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
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
                        name: "fk_user_profile_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
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
                        name: "fk_user_security_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
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
                        name: "fk_user_usage_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_member",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)2),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_member", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_member_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_member_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
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
                        name: "fk_organization_usage_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    @event = table.Column<string>(name: "event", type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    target_entity = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    target_id = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_audit_log_organization_member_actor_id",
                        column: x => x.actor_id,
                        principalTable: "organization_member",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_organization_audit_log_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "organization_invitation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_user_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    invited_user_role_id = table.Column<byte>(type: "smallint", nullable: false),
                    invited_user_permissions = table.Column<long>(type: "bigint", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)1),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW() + INTERVAL '5 days'"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    invited_user_role = table.Column<byte>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_invitation", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_invitation_organization_member_invited_by",
                        column: x => x.invited_by,
                        principalTable: "organization_member",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_invitation_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_team",
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
                    table.PrimaryKey("pk_organization_team", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_team_organization_member_team_manager_id",
                        column: x => x.team_manager_id,
                        principalTable: "organization_member",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_organization_team_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "short_url",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    short_code = table.Column<string>(type: "character varying(15)", unicode: false, maxLength: 15, nullable: true),
                    owner_type = table.Column<byte>(type: "smallint", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    anonymous_session_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    tracking_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    click_limit = table.Column<int>(type: "integer", nullable: false, defaultValue: -1),
                    total_clicks = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_password_protected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_private = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_short_url", x => x.id);
                    table.CheckConstraint("CK_ShortUrls_SingleOwner", "(\n    owner_type = 1\n    AND user_id IS NOT NULL\n    AND organization_id IS NULL\n    AND created_by_member_id IS NULL\n    AND anonymous_session_id IS NULL\n)\nOR\n(\n    owner_type = 2\n    AND user_id IS NULL\n    AND organization_id IS NOT NULL\n    AND created_by_member_id IS NOT NULL\n    AND anonymous_session_id IS NULL\n)\nOR\n(\n    owner_type = 3\n    AND user_id IS NULL\n    AND organization_id IS NULL\n    AND created_by_member_id IS NULL\n    AND anonymous_session_id IS NOT NULL\n)");
                    table.ForeignKey(
                        name: "fk_short_url_organization_member_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "organization_member",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_short_url_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_short_url_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "organization_team_member",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_team_member", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_team_member_organization_member_member_id",
                        column: x => x.member_id,
                        principalTable: "organization_member",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_team_member_organization_team_team_id",
                        column: x => x.team_id,
                        principalTable: "organization_team",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "click_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    short_url_id = table.Column<long>(type: "bigint", nullable: false),
                    clicked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", unicode: false, maxLength: 45, nullable: false),
                    session_id = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    referrer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    device_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    referrer_domain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    traffic_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_click_event", x => x.id);
                    table.ForeignKey(
                        name: "fk_click_event_short_url_short_url_id",
                        column: x => x.short_url_id,
                        principalTable: "short_url",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_Analytics_Covering",
                table: "click_event",
                columns: new[] { "short_url_id", "clicked_at" })
                .Annotation("Npgsql:IndexInclude", new[] { "browser", "city", "country", "device_type", "ip_address", "operating_system", "referrer_domain", "session_id", "traffic_source", "utm_campaign", "utm_medium", "utm_source", "utm_term", "utm_content" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShortUrlId",
                table: "click_event",
                column: "short_url_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_UserId_ClickedAt",
                table: "click_event",
                columns: new[] { "user_id", "clicked_at" },
                filter: "user_id IS NOT NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "short_url_id", "session_id", "country", "city", "browser", "operating_system", "device_type", "referrer_domain", "traffic_source", "utm_campaign", "utm_medium", "utm_source" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeTokens_ExpiresAt",
                table: "email_change_token",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeTokens_Token",
                table: "email_change_token",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeTokens_UserId",
                table: "email_change_token",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeTokens_UserId_IsUsed",
                table: "email_change_token",
                columns: new[] { "user_id", "is_used" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_is_active",
                table: "organization",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_organization_is_active_deleted_at",
                table: "organization",
                columns: new[] { "is_active", "deleted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_owner_id",
                table: "organization",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_owner_id_name",
                table: "organization",
                columns: new[] { "owner_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_subscription_plan_id",
                table: "organization",
                column: "subscription_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_audit_log_actor_id",
                table: "organization_audit_log",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_audit_log_organization_id",
                table: "organization_audit_log",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_audit_log_organization_id_time_stamp",
                table: "organization_audit_log",
                columns: new[] { "organization_id", "time_stamp" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_invitation_invited_by",
                table: "organization_invitation",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "ix_organization_invitation_organization_id_invited_user_email_",
                table: "organization_invitation",
                columns: new[] { "organization_id", "invited_user_email", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_member_organization_id_is_active",
                table: "organization_member",
                columns: new[] { "organization_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_member_organization_id_user_id",
                table: "organization_member",
                columns: new[] { "organization_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_member_user_id",
                table: "organization_member",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_team_organization_id_name",
                table: "organization_team",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_team_team_manager_id",
                table: "organization_team",
                column: "team_manager_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_team_member_member_id",
                table: "organization_team_member",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_team_member_team_id_member_id",
                table: "organization_team_member",
                columns: new[] { "team_id", "member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_token_hash",
                table: "refresh_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_user_id",
                table: "refresh_token",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_user_id_is_revoked_expires_at",
                table: "refresh_token",
                columns: new[] { "user_id", "is_revoked", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_short_url_created_by_id",
                table: "short_url",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_short_url_created_by_member_id",
                table: "short_url",
                column: "created_by_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_short_url_is_active_expires_at",
                table: "short_url",
                columns: new[] { "is_active", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_short_url_organization_id",
                table: "short_url",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_short_url_owner_type_anonymous_session_id",
                table: "short_url",
                columns: new[] { "owner_type", "anonymous_session_id" });

            migrationBuilder.CreateIndex(
                name: "ix_short_url_short_code",
                table: "short_url",
                column: "short_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_TotalClicks_Popular",
                table: "short_url",
                columns: new[] { "total_clicks", "user_id" },
                descending: new[] { true, false })
                .Annotation("Npgsql:IndexInclude", new[] { "id", "short_code", "original_url", "created_at", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_UserId_Analytics",
                table: "short_url",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true },
                filter: "user_id IS NOT NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "id", "short_code", "original_url", "total_clicks", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_UserId_OwnerType1",
                table: "short_url",
                column: "user_id",
                filter: "user_id IS NOT NULL AND owner_type = 1")
                .Annotation("Npgsql:IndexInclude", new[] { "id", "short_code", "original_url", "is_active", "expires_at", "created_at", "total_clicks" });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plan_name",
                table: "subscription_plan",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_email",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_is_deleted",
                table: "user",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_user_is_deleted_is_active",
                table: "user",
                columns: new[] { "is_deleted", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_user_subscription_plan_id",
                table: "user",
                column: "subscription_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_username",
                table: "user",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserActionTokens_TokenHash",
                table: "user_action_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserActionTokens_UserId",
                table: "user_action_token",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserActionTokens_UserId_TokenType_Used",
                table: "user_action_token",
                columns: new[] { "user_id", "token_type", "used" });

            migrationBuilder.CreateIndex(
                name: "ix_user_audit_log_time_stamp",
                table: "user_audit_log",
                column: "time_stamp");

            migrationBuilder.CreateIndex(
                name: "ix_user_audit_log_user_id",
                table: "user_audit_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_audit_log_user_id_time_stamp",
                table: "user_audit_log",
                columns: new[] { "user_id", "time_stamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "click_event");

            migrationBuilder.DropTable(
                name: "email_change_token");

            migrationBuilder.DropTable(
                name: "organization_audit_log");

            migrationBuilder.DropTable(
                name: "organization_invitation");

            migrationBuilder.DropTable(
                name: "organization_team_member");

            migrationBuilder.DropTable(
                name: "organization_usage");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "user_action_token");

            migrationBuilder.DropTable(
                name: "user_audit_log");

            migrationBuilder.DropTable(
                name: "user_profile");

            migrationBuilder.DropTable(
                name: "user_security");

            migrationBuilder.DropTable(
                name: "user_usage");

            migrationBuilder.DropTable(
                name: "short_url");

            migrationBuilder.DropTable(
                name: "organization_team");

            migrationBuilder.DropTable(
                name: "organization_member");

            migrationBuilder.DropTable(
                name: "organization");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "subscription_plan");
        }
    }
}

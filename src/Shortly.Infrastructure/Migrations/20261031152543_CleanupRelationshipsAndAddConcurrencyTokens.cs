using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupRelationshipsAndAddConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organization_invitations_organization_members",
                table: "organization_invitations");

            migrationBuilder.DropForeignKey(
                name: "fk_organization_invitations_organizations_organization_id1",
                table: "organization_invitations");

            migrationBuilder.DropForeignKey(
                name: "fk_organization_members_organizations_organization_id1",
                table: "organization_members");

            migrationBuilder.DropForeignKey(
                name: "fk_organization_teams_organizations",
                table: "organization_teams");

            migrationBuilder.DropForeignKey(
                name: "fk_organizations_user_owner_id1",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "fk_short_urls_organization_members_organization_member_id",
                table: "short_urls");

            migrationBuilder.DropForeignKey(
                name: "fk_short_urls_organizations_organization_id1",
                table: "short_urls");

            migrationBuilder.DropForeignKey(
                name: "fk_team_members_organization_members_member_id1",
                table: "team_members");

            migrationBuilder.DropForeignKey(
                name: "fk_user_action_tokens_user_user_id1",
                table: "user_action_tokens");

            migrationBuilder.DropTable(
                name: "organization_audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_user_action_tokens_user_id1",
                table: "user_action_tokens");

            migrationBuilder.DropIndex(
                name: "ix_team_members_member_id1",
                table: "team_members");

            migrationBuilder.DropIndex(
                name: "ix_short_urls_organization_id1",
                table: "short_urls");

            migrationBuilder.DropIndex(
                name: "ix_short_urls_organization_member_id",
                table: "short_urls");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_organization_id",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_organizations_owner_id1",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_organization_members_organization_id1",
                table: "organization_members");

            migrationBuilder.DropIndex(
                name: "ix_organization_invitations_organization_id1",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "user_id1",
                table: "user_action_tokens");

            migrationBuilder.DropColumn(
                name: "member_id1",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "organization_id1",
                table: "short_urls");

            migrationBuilder.DropColumn(
                name: "organization_member_id",
                table: "short_urls");

            migrationBuilder.DropColumn(
                name: "owner_id1",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "organization_id1",
                table: "organization_members");

            migrationBuilder.DropColumn(
                name: "invited_user_role_id",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "organization_id1",
                table: "organization_invitations");

            migrationBuilder.RenameColumn(
                name: "en_subscription_plan_id",
                table: "users",
                newName: "subscription_plan_id");

            migrationBuilder.RenameIndex(
                name: "ix_users_en_subscription_plan_id",
                table: "users",
                newName: "ix_users_subscription_plan_id");

            migrationBuilder.RenameColumn(
                name: "en_subscription_plan_id",
                table: "organizations",
                newName: "subscription_plan_id");

            migrationBuilder.RenameIndex(
                name: "ix_organizations_en_subscription_plan_id",
                table: "organizations",
                newName: "ix_organizations_subscription_plan_id");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "user_usage",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "user_security",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "organization_usage",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<byte>(
                name: "invited_user_role",
                table: "organization_invitations",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                table: "organization_invitations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "NOW() + INTERVAL '5 days'");

            migrationBuilder.AddColumn<string>(
                name: "token_hash",
                table: "organization_invitations",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_organization_id",
                table: "refresh_tokens",
                column: "organization_id",
                filter: "organization_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_organization_invitations_token_hash",
                table: "organization_invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_organization_invitations_users",
                table: "organization_invitations",
                column: "invited_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_organization_teams_organizations",
                table: "organization_teams",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organization_invitations_users",
                table: "organization_invitations");

            migrationBuilder.DropForeignKey(
                name: "fk_organization_teams_organizations",
                table: "organization_teams");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_organization_id",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ux_organization_invitations_token_hash",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "user_usage");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "user_security");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "organization_usage");

            migrationBuilder.DropColumn(
                name: "token_hash",
                table: "organization_invitations");

            migrationBuilder.RenameColumn(
                name: "subscription_plan_id",
                table: "users",
                newName: "en_subscription_plan_id");

            migrationBuilder.RenameIndex(
                name: "ix_users_subscription_plan_id",
                table: "users",
                newName: "ix_users_en_subscription_plan_id");

            migrationBuilder.RenameColumn(
                name: "subscription_plan_id",
                table: "organizations",
                newName: "en_subscription_plan_id");

            migrationBuilder.RenameIndex(
                name: "ix_organizations_subscription_plan_id",
                table: "organizations",
                newName: "ix_organizations_en_subscription_plan_id");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id1",
                table: "user_action_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "member_id1",
                table: "team_members",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id1",
                table: "short_urls",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "organization_member_id",
                table: "short_urls",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_id1",
                table: "organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id1",
                table: "organization_members",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "invited_user_role",
                table: "organization_invitations",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                table: "organization_invitations",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "NOW() + INTERVAL '5 days'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<byte>(
                name: "invited_user_role_id",
                table: "organization_invitations",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id1",
                table: "organization_invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "organization_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    @event = table.Column<string>(name: "event", type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_entity = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    target_id = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
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

            migrationBuilder.CreateIndex(
                name: "ix_user_action_tokens_user_id1",
                table: "user_action_tokens",
                column: "user_id1");

            migrationBuilder.CreateIndex(
                name: "ix_team_members_member_id1",
                table: "team_members",
                column: "member_id1");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_organization_id1",
                table: "short_urls",
                column: "organization_id1");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_organization_member_id",
                table: "short_urls",
                column: "organization_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_organization_id",
                table: "refresh_tokens",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_owner_id1",
                table: "organizations",
                column: "owner_id1");

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_organization_id1",
                table: "organization_members",
                column: "organization_id1");

            migrationBuilder.CreateIndex(
                name: "ix_organization_invitations_organization_id1",
                table: "organization_invitations",
                column: "organization_id1");

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

            migrationBuilder.AddForeignKey(
                name: "fk_organization_invitations_organization_members",
                table: "organization_invitations",
                column: "invited_by",
                principalTable: "organization_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_organization_invitations_organizations_organization_id1",
                table: "organization_invitations",
                column: "organization_id1",
                principalTable: "organizations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_organization_members_organizations_organization_id1",
                table: "organization_members",
                column: "organization_id1",
                principalTable: "organizations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_organization_teams_organizations",
                table: "organization_teams",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_organizations_user_owner_id1",
                table: "organizations",
                column: "owner_id1",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_short_urls_organization_members_organization_member_id",
                table: "short_urls",
                column: "organization_member_id",
                principalTable: "organization_members",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_short_urls_organizations_organization_id1",
                table: "short_urls",
                column: "organization_id1",
                principalTable: "organizations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_team_members_organization_members_member_id1",
                table: "team_members",
                column: "member_id1",
                principalTable: "organization_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_action_tokens_user_user_id1",
                table: "user_action_tokens",
                column: "user_id1",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRelationshipsAndUtcTimestampColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organization_members_users_user_id1",
                table: "organization_members");

            migrationBuilder.DropForeignKey(
                name: "fk_short_urls_user_user_id1",
                table: "short_urls");

            migrationBuilder.DropForeignKey(
                name: "fk_short_urls_users",
                table: "short_urls");

            migrationBuilder.DropForeignKey(
                name: "fk_user_audit_logs_user_user_id1",
                table: "user_audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_user_audit_logs_user_id1",
                table: "user_audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_short_urls_user_id1",
                table: "short_urls");

            migrationBuilder.DropIndex(
                name: "ix_organization_members_user_id1",
                table: "organization_members");

            migrationBuilder.DropColumn(
                name: "user_id1",
                table: "user_audit_logs");

            migrationBuilder.DropColumn(
                name: "user_id1",
                table: "short_urls");

            migrationBuilder.DropColumn(
                name: "user_id1",
                table: "organization_members");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "users",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "subscription_plan_id",
                table: "users",
                newName: "en_subscription_plan_id");

            migrationBuilder.RenameColumn(
                name: "last_login_at",
                table: "users",
                newName: "last_login_at_utc");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "users",
                newName: "deleted_at_utc");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "users",
                newName: "created_at_utc");

            migrationBuilder.RenameIndex(
                name: "ix_users_subscription_plan_id",
                table: "users",
                newName: "ix_users_en_subscription_plan_id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "user_security",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "locked_until",
                table: "user_security",
                newName: "locked_until_utc");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "user_profile",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "time_stamp",
                table: "user_audit_logs",
                newName: "time_stamp_utc");

            migrationBuilder.RenameColumn(
                name: "subscription_plan_id",
                table: "organizations",
                newName: "en_subscription_plan_id");

            migrationBuilder.RenameIndex(
                name: "ix_organizations_subscription_plan_id",
                table: "organizations",
                newName: "ix_organizations_en_subscription_plan_id");

            migrationBuilder.AddForeignKey(
                name: "fk_short_urls_users",
                table: "short_urls",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_short_urls_users",
                table: "short_urls");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "last_login_at_utc",
                table: "users",
                newName: "last_login_at");

            migrationBuilder.RenameColumn(
                name: "en_subscription_plan_id",
                table: "users",
                newName: "subscription_plan_id");

            migrationBuilder.RenameColumn(
                name: "deleted_at_utc",
                table: "users",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "ix_users_en_subscription_plan_id",
                table: "users",
                newName: "ix_users_subscription_plan_id");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "user_security",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "locked_until_utc",
                table: "user_security",
                newName: "locked_until");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "user_profile",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "time_stamp_utc",
                table: "user_audit_logs",
                newName: "time_stamp");

            migrationBuilder.RenameColumn(
                name: "en_subscription_plan_id",
                table: "organizations",
                newName: "subscription_plan_id");

            migrationBuilder.RenameIndex(
                name: "ix_organizations_en_subscription_plan_id",
                table: "organizations",
                newName: "ix_organizations_subscription_plan_id");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id1",
                table: "user_audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id1",
                table: "short_urls",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id1",
                table: "organization_members",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_user_audit_logs_user_id1",
                table: "user_audit_logs",
                column: "user_id1");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_user_id1",
                table: "short_urls",
                column: "user_id1");

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_user_id1",
                table: "organization_members",
                column: "user_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_organization_members_users_user_id1",
                table: "organization_members",
                column: "user_id1",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_short_urls_user_user_id1",
                table: "short_urls",
                column: "user_id1",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_short_urls_users",
                table: "short_urls",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_audit_logs_user_user_id1",
                table: "user_audit_logs",
                column: "user_id1",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}

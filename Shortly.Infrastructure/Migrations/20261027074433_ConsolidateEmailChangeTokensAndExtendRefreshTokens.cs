using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEmailChangeTokensAndExtendRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_change_token");

            migrationBuilder.AddColumn<string>(
                name: "new_email",
                table: "user_action_token",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "used_at",
                table: "user_action_token",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "refresh_token",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "new_email",
                table: "user_action_token");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "user_action_token");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "refresh_token");

            migrationBuilder.CreateTable(
                name: "email_change_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    new_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    old_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
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
        }
    }
}

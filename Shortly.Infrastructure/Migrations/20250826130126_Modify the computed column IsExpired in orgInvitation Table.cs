using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModifythecomputedcolumnIsExpiredinorgInvitationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsExpired",
                table: "OrganizationInvitations",
                type: "bit",
                nullable: false,
                computedColumnSql: "CASE WHEN [ExpiresAt] < GETUTCDATE() OR [Status] IN (3,5,6) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComputedColumnSql: "CASE WHEN [ExpiresAt] < GETUTCDATE() THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsExpired",
                table: "OrganizationInvitations",
                type: "bit",
                nullable: false,
                computedColumnSql: "CASE WHEN [ExpiresAt] < GETUTCDATE() THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComputedColumnSql: "CASE WHEN [ExpiresAt] < GETUTCDATE() OR [Status] IN (3,5,6) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvitationtokencolfrominvitationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationInvitations_InvitationToken",
                table: "OrganizationInvitations");

            migrationBuilder.DropColumn(
                name: "InvitationToken",
                table: "OrganizationInvitations");

            migrationBuilder.AlterColumn<long>(
                name: "Permissions",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 8725724278095887L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 15L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)1,
                column: "DefaultPermissions",
                value: 4785074604146688L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)2,
                column: "DefaultPermissions",
                value: 8725724815097871L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)3,
                column: "DefaultPermissions",
                value: 8725726157306911L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)4,
                column: "DefaultPermissions",
                value: 8944576469367839L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)5,
                column: "DefaultPermissions",
                value: 8945489149918239L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)6,
                column: "DefaultPermissions",
                value: -63112104888009697L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Permissions",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 15L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 8725724278095887L);

            migrationBuilder.AddColumn<string>(
                name: "InvitationToken",
                table: "OrganizationInvitations",
                type: "varchar(256)",
                unicode: false,
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)1,
                column: "DefaultPermissions",
                value: 276824576L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)2,
                column: "DefaultPermissions",
                value: 528515839L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)3,
                column: "DefaultPermissions",
                value: 530579199L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)4,
                column: "DefaultPermissions",
                value: 19327352575L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)5,
                column: "DefaultPermissions",
                value: -1L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)6,
                column: "DefaultPermissions",
                value: -1L);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_InvitationToken",
                table: "OrganizationInvitations",
                column: "InvitationToken",
                unique: true);
        }
    }
}

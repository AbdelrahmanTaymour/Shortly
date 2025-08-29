using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResonFeildinSecurtyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LockoutReason",
                table: "UserSecurity",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)1,
                column: "DefaultPermissions",
                value: 138412160L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)2,
                column: "DefaultPermissions",
                value: 264257791L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)3,
                column: "DefaultPermissions",
                value: 265289727L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)4,
                column: "DefaultPermissions",
                value: 18253611007L);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockoutReason",
                table: "UserSecurity");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)1,
                column: "DefaultPermissions",
                value: 17716740224L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)2,
                column: "DefaultPermissions",
                value: 33822900479L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)3,
                column: "DefaultPermissions",
                value: 33824964607L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)4,
                column: "DefaultPermissions",
                value: 13193611051007L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)5,
                column: "DefaultPermissions",
                value: -562949953421313L);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)6,
                column: "DefaultPermissions",
                value: -562949953421313L);
        }
    }
}

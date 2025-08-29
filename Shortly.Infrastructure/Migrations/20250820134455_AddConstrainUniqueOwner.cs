using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConstrainUniqueOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OwnerId_Name",
                table: "Organizations",
                columns: new[] { "OwnerId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_OwnerId_Name",
                table: "Organizations");

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
                value: 9663676415L);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIpAddressToShortUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "ShortUrls",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "ShortUrls");
        }
    }
}

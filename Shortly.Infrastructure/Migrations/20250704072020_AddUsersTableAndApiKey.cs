using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersTableAndApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "ShortUrls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ShortUrls",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_UserId",
                table: "ShortUrls",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShortUrls_Users_UserId",
                table: "ShortUrls",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShortUrls_Users_UserId",
                table: "ShortUrls");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_UserId",
                table: "ShortUrls");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "ShortUrls");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ShortUrls");
        }
    }
}

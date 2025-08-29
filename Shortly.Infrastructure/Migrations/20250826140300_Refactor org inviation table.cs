using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Refactororginviationtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "InvitedUserPermissions",
                table: "OrganizationInvitations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte>(
                name: "InvitedUserRoleId",
                table: "OrganizationInvitations",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_InvitedUserRoleId",
                table: "OrganizationInvitations",
                column: "InvitedUserRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationInvitations_Roles_InvitedUserRoleId",
                table: "OrganizationInvitations",
                column: "InvitedUserRoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationInvitations_Roles_InvitedUserRoleId",
                table: "OrganizationInvitations");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationInvitations_InvitedUserRoleId",
                table: "OrganizationInvitations");

            migrationBuilder.DropColumn(
                name: "InvitedUserPermissions",
                table: "OrganizationInvitations");

            migrationBuilder.DropColumn(
                name: "InvitedUserRoleId",
                table: "OrganizationInvitations");
        }
    }
}

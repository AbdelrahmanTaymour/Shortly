using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Madetheteammemberscascadeondelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationTeamMembers_OrganizationTeams_TeamId",
                table: "OrganizationTeamMembers");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationTeamMembers_OrganizationTeams_TeamId",
                table: "OrganizationTeamMembers",
                column: "TeamId",
                principalTable: "OrganizationTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationTeamMembers_OrganizationTeams_TeamId",
                table: "OrganizationTeamMembers");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationTeamMembers_OrganizationTeams_TeamId",
                table: "OrganizationTeamMembers",
                column: "TeamId",
                principalTable: "OrganizationTeams",
                principalColumn: "Id");
        }
    }
}

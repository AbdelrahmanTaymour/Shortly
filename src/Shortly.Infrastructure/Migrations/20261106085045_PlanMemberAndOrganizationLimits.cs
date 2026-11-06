using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMemberAndOrganizationLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_members_per_organization",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "max_organizations",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: (byte)1,
                columns: new[] { "max_members_per_organization", "max_organizations" },
                values: new object[] { 3, 1 });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: (byte)2,
                columns: new[] { "max_members_per_organization", "max_organizations" },
                values: new object[] { 10, 3 });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: (byte)3,
                columns: new[] { "max_members_per_organization", "max_organizations" },
                values: new object[] { 50, 10 });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: (byte)4,
                columns: new[] { "max_members_per_organization", "max_organizations" },
                values: new object[] { -1, -1 });

            // An organization's member limit now follows its plan (an unlimited plan stores int.MaxValue).
            // Members above a lower limit keep their access; only new members are blocked.
            migrationBuilder.Sql("""
                UPDATE organizations o
                SET member_limit = CASE WHEN p.max_members_per_organization < 0 THEN 2147483647 ELSE p.max_members_per_organization END
                FROM subscription_plans p
                WHERE p.id = o.subscription_plan_id;
                """);

            // Invitation emails are now stored trimmed and in lower case.
            migrationBuilder.Sql("""
                UPDATE organization_invitations
                SET invited_user_email = lower(trim(invited_user_email));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_members_per_organization",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "max_organizations",
                table: "subscription_plans");
        }
    }
}

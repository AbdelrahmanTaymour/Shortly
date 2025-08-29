using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Shortly.Domain.Enums;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addorganizationusgaetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add column as nullable first
            migrationBuilder.AddColumn<byte>(
                name: "SubscriptionPlanId",
                table: "Organizations",
                type: "tinyint",
                nullable: true,
                defaultValue: (byte)0);
            
            // 2. Update existing orgs -> Enterprise plan (enum value = enSubscriptionPlan.Enterprise)
            migrationBuilder.Sql($@"
                UPDATE Organizations
                SET SubscriptionPlanId = {(byte)enSubscriptionPlan.Enterprise}
                WHERE SubscriptionPlanId IS NULL
            ");
            
            // 3. Alter column to NOT NULL
            migrationBuilder.AlterColumn<byte>(
                name: "SubscriptionPlanId",
                table: "Organizations",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(byte),
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationUsage",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonthlyLinksCreated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MonthlyQrCodesCreated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalLinksCreated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalQrCodesCreated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MonthlyResetDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(month, 1, GETUTCDATE())"),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUsage", x => x.OrganizationId);
                    table.ForeignKey(
                        name: "FK_OrganizationUsage_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_SubscriptionPlanId",
                table: "Organizations",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsage_OrganizationId",
                table: "OrganizationUsage",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_SubscriptionPlans_SubscriptionPlanId",
                table: "Organizations",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_SubscriptionPlans_SubscriptionPlanId",
                table: "Organizations");

            migrationBuilder.DropTable(
                name: "OrganizationUsage");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_SubscriptionPlanId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "Organizations");
        }
    }
}

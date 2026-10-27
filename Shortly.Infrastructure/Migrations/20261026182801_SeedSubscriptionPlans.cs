using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "subscription_plan",
                columns: new[] { "id", "bulk_creation", "campaign_tracking", "click_data_retention_days", "custom_short_code", "description", "geo_device_tracking", "link_analysis", "link_protection", "max_links_per_month", "max_qr_codes_per_month", "name", "price" },
                values: new object[,]
                {
                    { (byte)1, false, false, 0, false, "Try it out for free", false, false, false, 5, 2, "Free", 0m },
                    { (byte)2, false, false, 30, true, "Unlock powerful data", false, true, false, 100, 5, "Starter", 10m },
                    { (byte)3, true, true, 90, true, "For growing teams", false, true, true, 500, 10, "Professional", 50m },
                    { (byte)4, true, true, -1, true, "Unlimited everything", true, true, true, -1, -1, "Enterprise", 200m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "subscription_plan",
                keyColumn: "id",
                keyValue: (byte)1);

            migrationBuilder.DeleteData(
                table: "subscription_plan",
                keyColumn: "id",
                keyValue: (byte)2);

            migrationBuilder.DeleteData(
                table: "subscription_plan",
                keyColumn: "id",
                keyValue: (byte)3);

            migrationBuilder.DeleteData(
                table: "subscription_plan",
                keyColumn: "id",
                keyValue: (byte)4);
        }
    }
}

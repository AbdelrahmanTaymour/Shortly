using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addedindesies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_UserId",
                table: "ShortUrls");

            migrationBuilder.DropIndex(
                name: "IX_ClickEvents_ShortUrlId_ClickedAt",
                table: "ClickEvents");

            migrationBuilder.DropIndex(
                name: "IX_ClickEvents_ShortUrlId_Country",
                table: "ClickEvents");

            migrationBuilder.DropIndex(
                name: "IX_ClickEvents_ShortUrlId_DeviceType",
                table: "ClickEvents");

            migrationBuilder.DropIndex(
                name: "IX_ClickEvents_ShortUrlId_TrafficSource",
                table: "ClickEvents");

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_TotalClicks_Popular",
                table: "ShortUrls",
                columns: new[] { "TotalClicks", "UserId" },
                descending: new[] { true, false })
                .Annotation("SqlServer:Include", new[] { "Id", "ShortCode", "OriginalUrl", "CreatedAt", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_UserId_Analytics",
                table: "ShortUrls",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true },
                filter: "[UserId] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Id", "ShortCode", "OriginalUrl", "TotalClicks", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_Analytics_Covering",
                table: "ClickEvents",
                columns: new[] { "ShortUrlId", "ClickedAt" })
                .Annotation("SqlServer:Include", new[] { "Country", "City", "DeviceType", "Browser", "OperatingSystem", "TrafficSource", "ReferrerDomain", "IpAddress", "SessionId", "UtmSource", "UtmCampaign" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_TotalClicks_Popular",
                table: "ShortUrls");

            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_UserId_Analytics",
                table: "ShortUrls");

            migrationBuilder.DropIndex(
                name: "IX_ClickEvents_Analytics_Covering",
                table: "ClickEvents");

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_UserId",
                table: "ShortUrls",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShortUrlId_ClickedAt",
                table: "ClickEvents",
                columns: new[] { "ShortUrlId", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShortUrlId_Country",
                table: "ClickEvents",
                columns: new[] { "ShortUrlId", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShortUrlId_DeviceType",
                table: "ClickEvents",
                columns: new[] { "ShortUrlId", "DeviceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShortUrlId_TrafficSource",
                table: "ClickEvents",
                columns: new[] { "ShortUrlId", "TrafficSource" });
        }
    }
}

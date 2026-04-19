using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Seeded_Admin_User : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsOAuthUser",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "DeletedBy", "Email", "GoogleId", "GoogleProfilePicture", "IsActive", "IsEmailConfirmed", "LastLoginAt", "PasswordHash", "Permissions", "SubscriptionPlanId", "UpdatedAt", "Username" },
                values: new object[] { new Guid("d27b9c92-747d-4b9d-bc65-083656729e24"), new DateTime(2025, 12, 12, 11, 32, 20, 0, DateTimeKind.Utc), null, null, "taymour@gmail.com", null, null, true, true, null, "$2a$10$0EjZyXu5orwyTJ2GZyVRWeRSL0bnR.FrPx0aFi5GORCoDWLVNR6Ca", -1L, (byte)3, new DateTime(2025, 12, 12, 11, 32, 20, 0, DateTimeKind.Utc), "taymour" });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "UserId", "Bio", "Company", "Country", "Location", "Name", "PhoneNumber", "ProfilePictureUrl", "TimeZone", "UpdatedAt", "Website" },
                values: new object[] { new Guid("d27b9c92-747d-4b9d-bc65-083656729e24"), null, null, null, null, "Abdelrahman Taymour", null, null, null, new DateTime(2025, 12, 12, 11, 32, 20, 0, DateTimeKind.Utc), null });

            migrationBuilder.InsertData(
                table: "UserSecurity",
                columns: new[] { "UserId", "LockedUntil", "LockoutReason", "TwoFactorSecret", "UpdatedAt" },
                values: new object[] { new Guid("d27b9c92-747d-4b9d-bc65-083656729e24"), null, null, null, new DateTime(2025, 12, 12, 11, 32, 20, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "UserUsage",
                columns: new[] { "UserId", "MonthlyResetDate" },
                values: new object[] { new Guid("d27b9c92-747d-4b9d-bc65-083656729e24"), new DateTime(2026, 1, 12, 11, 32, 20, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "UserId",
                keyValue: new Guid("d27b9c92-747d-4b9d-bc65-083656729e24"));

            migrationBuilder.DeleteData(
                table: "UserSecurity",
                keyColumn: "UserId",
                keyValue: new Guid("d27b9c92-747d-4b9d-bc65-083656729e24"));

            migrationBuilder.DeleteData(
                table: "UserUsage",
                keyColumn: "UserId",
                keyValue: new Guid("d27b9c92-747d-4b9d-bc65-083656729e24"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d27b9c92-747d-4b9d-bc65-083656729e24"));

            migrationBuilder.AlterColumn<bool>(
                name: "IsOAuthUser",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);
        }
    }
}

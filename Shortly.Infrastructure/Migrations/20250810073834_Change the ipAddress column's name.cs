using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangetheipAddresscolumnsname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ShortUrls_CreatedByMember",
                table: "ShortUrls");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ShortUrls_SingleOwner",
                table: "ShortUrls");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)4,
                column: "DefaultPermissions",
                value: 9663676415L);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShortUrls_SingleOwner",
                table: "ShortUrls",
                sql: "\n            (\n                -- User owned\n                [OwnerType] = 1\n                AND [UserId] IS NOT NULL\n                AND [OrganizationId] IS NULL\n                AND [CreatedByMemberId] IS NULL\n                AND [AnonymousSessionId] IS NULL\n                )\n                OR\n                (\n                    -- Organization owned\n                    [OwnerType] = 2\n                    AND [UserId] IS NULL\n                    AND [OrganizationId] IS NOT NULL\n                    AND [CreatedByMemberId] IS NOT NULL\n                    AND [AnonymousSessionId] IS NULL\n                )\n                OR\n                (\n                    -- Anonymous owned\n                    [OwnerType] = 3\n                    AND [UserId] IS NULL\n                    AND [OrganizationId] IS NULL\n                    AND [CreatedByMemberId] IS NULL\n                    AND [AnonymousSessionId] IS NOT NULL\n            )\n        ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ShortUrls_SingleOwner",
                table: "ShortUrls");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)4,
                column: "DefaultPermissions",
                value: 18253611007L);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShortUrls_CreatedByMember",
                table: "ShortUrls",
                sql: "(\n            -- If OrganizationId is set, CreatedByMemberId should be set\n            ([OrganizationId] IS NOT NULL AND [CreatedByMemberId] IS NOT NULL)\n            OR\n            -- If OrganizationId is null, CreatedByMemberId should be null\n            ([OrganizationId] IS NULL AND [CreatedByMemberId] IS NULL)\n        )");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShortUrls_SingleOwner",
                table: "ShortUrls",
                sql: "(\n            -- User owned: OwnerType = 1, UserId is set, OrganizationId is null, no anonymous fields\n            ([OwnerType] = 1 AND [UserId] IS NOT NULL AND [OrganizationId] IS NULL AND [AnonymousSessionId] IS NULL AND [IpAddress] IS NULL)\n            OR\n            -- Organization owned: OwnerType = 2, OrganizationId is set, UserId is null, no anonymous fields  \n            ([OwnerType] = 2 AND [OrganizationId] IS NOT NULL AND [UserId] IS NULL AND [AnonymousSessionId] IS NULL AND [IpAddress] IS NULL)\n            OR\n            -- Anonymous owned: OwnerType = 3, Both UserId and OrganizationId are null, anonymous fields can be set\n            ([OwnerType] = 3 AND [UserId] IS NULL AND [OrganizationId] IS NULL)\n        )");
        }
    }
}

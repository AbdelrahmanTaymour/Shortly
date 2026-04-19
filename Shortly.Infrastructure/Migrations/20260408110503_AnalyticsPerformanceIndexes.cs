using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shortly.Infrastructure.Migrations;

public partial class AnalyticsPerformanceIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name:  "IX_ClickEvents_Analytics_Covering",
            table: "ClickEvents");

        migrationBuilder.Sql("""
            CREATE NONCLUSTERED INDEX IX_ClickEvents_Analytics_Covering
            ON dbo.ClickEvents (ShortUrlId ASC, ClickedAt ASC)
            INCLUDE
            (
                Browser,
                City,
                Country,
                DeviceType,
                IpAddress,
                OperatingSystem,
                ReferrerDomain,
                SessionId,
                TrafficSource,
                UtmCampaign,
                UtmMedium,
                UtmSource,
                UtmTerm,
                UtmContent
            )
            WITH (ONLINE = ON, FILLFACTOR = 90, SORT_IN_TEMPDB = ON);
            """);

        migrationBuilder.AddColumn<Guid>(
            name:       "UserId",
            table:      "ClickEvents",
            type:       "uniqueidentifier",
            nullable:   true,
            defaultValue: null);

        migrationBuilder.Sql("""
            DECLARE @BatchSize INT = 10000;
            DECLARE @Rows      INT = 1;

            WHILE @Rows > 0
            BEGIN
                UPDATE TOP (@BatchSize) ce
                SET    ce.UserId = su.UserId
                FROM   dbo.ClickEvents ce
                JOIN   dbo.ShortUrls   su
                       ON ce.ShortUrlId = su.Id
                WHERE  ce.UserId IS NULL
                  AND  su.UserId IS NOT NULL;

                SET @Rows = @@ROWCOUNT;

                IF @Rows > 0
                    WAITFOR DELAY '00:00:00.050';
            END
            """);

        migrationBuilder.Sql("""
            CREATE NONCLUSTERED INDEX IX_ClickEvents_UserId_ClickedAt
            ON dbo.ClickEvents (UserId ASC, ClickedAt ASC)
            INCLUDE
            (
                ShortUrlId,
                SessionId,
                Country,
                City,
                Browser,
                OperatingSystem,
                DeviceType,
                ReferrerDomain,
                TrafficSource,
                UtmCampaign,
                UtmMedium,
                UtmSource
            )
            WHERE UserId IS NOT NULL
            WITH (ONLINE = ON, FILLFACTOR = 90, SORT_IN_TEMPDB = ON);
            """);

        migrationBuilder.Sql("""
            CREATE NONCLUSTERED INDEX IX_ShortUrls_UserId_OwnerType1
            ON dbo.ShortUrls (UserId ASC)
            INCLUDE
            (
                Id,
                ShortCode,
                OriginalUrl,
                IsActive,
                ExpiresAt,
                CreatedAt,
                TotalClicks
            )
            WHERE UserId IS NOT NULL AND OwnerType = 1
            WITH (ONLINE = ON, FILLFACTOR = 100, SORT_IN_TEMPDB = ON);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX IF EXISTS IX_ShortUrls_UserId_OwnerType1 ON dbo.ShortUrls;");

        migrationBuilder.Sql(
            "DROP INDEX IF EXISTS IX_ClickEvents_UserId_ClickedAt ON dbo.ClickEvents;");

        migrationBuilder.DropColumn(
            name:  "UserId",
            table: "ClickEvents");

        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS IX_ClickEvents_Analytics_Covering ON dbo.ClickEvents;

            CREATE NONCLUSTERED INDEX IX_ClickEvents_Analytics_Covering
            ON dbo.ClickEvents (ShortUrlId ASC, ClickedAt ASC)
            INCLUDE
            (
                Browser,
                City,
                Country,
                DeviceType,
                IpAddress,
                OperatingSystem,
                ReferrerDomain,
                SessionId,
                TrafficSource,
                UtmCampaign,
                UtmSource
            )
            WITH (ONLINE = ON, FILLFACTOR = 100);
            """);
    }
}
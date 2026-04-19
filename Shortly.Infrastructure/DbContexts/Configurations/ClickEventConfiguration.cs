using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

// TODO: Consider partitioning this table by date for large datasets

public class ClickEventConfiguration : IEntityTypeConfiguration<ClickEvent>
{
    public void Configure(EntityTypeBuilder<ClickEvent> builder)
    {
        // Primary key
        builder.HasKey(ce => ce.Id);

        // Properties
        builder.Property(ce => ce.ClickedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(ce => ce.IpAddress).HasMaxLength(45).IsUnicode(false);
        builder.Property(ce => ce.SessionId).HasMaxLength(128).IsUnicode(false);
        builder.Property(ce => ce.UserAgent).HasMaxLength(500);
        builder.Property(ce => ce.Country).HasMaxLength(100);
        builder.Property(ce => ce.City).HasMaxLength(80);
        builder.Property(ce => ce.Browser).HasMaxLength(50);
        builder.Property(ce => ce.OperatingSystem).HasMaxLength(50);
        builder.Property(ce => ce.Device).HasMaxLength(50);
        builder.Property(ce => ce.DeviceType).HasMaxLength(20);
        builder.Property(ce => ce.Referrer).HasMaxLength(500);
        builder.Property(ce => ce.ReferrerDomain).HasMaxLength(100);
        builder.Property(ce => ce.TrafficSource).HasMaxLength(50);

        // UTM parameters
        builder.Property(ce => ce.UtmSource).HasMaxLength(100);
        builder.Property(ce => ce.UtmMedium).HasMaxLength(100);
        builder.Property(ce => ce.UtmCampaign).HasMaxLength(100);
        builder.Property(ce => ce.UtmTerm).HasMaxLength(100);
        builder.Property(ce => ce.UtmContent).HasMaxLength(100);

        // Denormalized UserId — plain column, no FK relationship to Users.
        // Intentionally unconstrained so a user deletion does not cascade
        // into ClickEvents; historical analytics data is preserved.
        builder.Property(ce => ce.UserId).IsRequired(false);

        // ── Indexes ───────────────────────────────────────────────────────────

        builder.HasIndex(c => c.ShortUrlId)
            .HasDatabaseName("IX_ClickEvents_ShortUrlId");

        builder.HasIndex(c => new { c.ShortUrlId, c.ClickedAt })
            .HasDatabaseName("IX_ClickEvents_Analytics_Covering")
            .IncludeProperties(c => new
            {
                c.Browser,
                c.City,
                c.Country,
                c.DeviceType,
                c.IpAddress,
                c.OperatingSystem,
                c.ReferrerDomain,
                c.SessionId,
                c.TrafficSource,
                c.UtmCampaign,
                c.UtmMedium,   // ← was missing; caused Key Lookup per campaign row
                c.UtmSource,
                c.UtmTerm,     // ← added for completeness
                c.UtmContent   // ← added for completeness
            });

        builder.HasIndex(c => new { c.UserId, c.ClickedAt })
            .HasDatabaseName("IX_ClickEvents_UserId_ClickedAt")
            .HasFilter("[UserId] IS NOT NULL")
            .IncludeProperties(c => new
            {
                c.ShortUrlId,
                c.SessionId,
                c.Country,
                c.City,
                c.Browser,
                c.OperatingSystem,
                c.DeviceType,
                c.ReferrerDomain,
                c.TrafficSource,
                c.UtmCampaign,
                c.UtmMedium,
                c.UtmSource
            });
    }
}
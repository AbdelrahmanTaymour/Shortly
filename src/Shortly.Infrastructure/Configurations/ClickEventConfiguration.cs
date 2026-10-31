using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.ClickEvents;
using Shortly.Domain.ShortUrls;

namespace Shortly.Infrastructure.Configurations;

public sealed class ClickEventConfiguration : IEntityTypeConfiguration<ClickEvent>
{
    public void Configure(EntityTypeBuilder<ClickEvent> builder)
    {
        builder.ToTable("click_events");

        builder.HasKey(ce => ce.Id);

        builder.Property(ce => ce.ClickedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
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

        builder.Property(ce => ce.UtmSource).HasMaxLength(100);
        builder.Property(ce => ce.UtmMedium).HasMaxLength(100);
        builder.Property(ce => ce.UtmCampaign).HasMaxLength(100);
        builder.Property(ce => ce.UtmTerm).HasMaxLength(100);
        builder.Property(ce => ce.UtmContent).HasMaxLength(100);

        builder.HasIndex(ce => new { ce.ShortUrlId, ce.ClickedAtUtc })
            .HasDatabaseName("ix_click_events_short_url_id_clicked_at");

        builder.HasOne<ShortUrl>()
            .WithMany()
            .HasForeignKey(ce => ce.ShortUrlId)
            .HasConstraintName("fk_click_events_short_urls")
            .OnDelete(DeleteBehavior.Cascade); // clicks have no meaning once their URL is gone
    }
}
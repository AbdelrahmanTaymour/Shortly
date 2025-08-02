using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

        //TODO: Consider partitioning this table by date for large datasets

public class ClickEventConfiguration:IEntityTypeConfiguration<ClickEvent>
{
    public void Configure(EntityTypeBuilder<ClickEvent> builder)
    {
        // Primary key
        builder.HasKey(ce => ce.Id);
        
        // Properties configuration
        builder.Property(ce => ce.ClickedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(ce => ce.IpAddress).HasMaxLength(45).IsUnicode(false);
        builder.Property(ce => ce.SessionId).HasMaxLength(128).IsUnicode(false);
        builder.Property(ce => ce.UserAgent).HasMaxLength(500);
        builder.Property(ce => ce.Country).HasMaxLength(50);
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
        
        // Indexes for analytics queries
        builder.HasIndex(c => c.ShortUrlId);
        builder.HasIndex(c => new { c.ShortUrlId, c.ClickedAt });
        builder.HasIndex(c => new { c.ShortUrlId, c.Country });
        builder.HasIndex(c => new { c.ShortUrlId, c.DeviceType });
        builder.HasIndex(c => new { c.ShortUrlId, c.TrafficSource });

    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

/// <summary>
///     Provides the entity configuration for the ShortUrl model in the database.
/// </summary>
public class ShortUrlConfiguration : IEntityTypeConfiguration<ShortUrl>
{
    public void Configure(EntityTypeBuilder<ShortUrl> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(e => e.OriginalUrl).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.ShortCode).HasMaxLength(15).IsUnicode(false);
        builder.Property(su => su.OwnerType).HasConversion<byte>();
        builder.Property(s => s.AnonymousSessionId).HasMaxLength(128);
        builder.Property(s => s.IpAddress).HasMaxLength(45);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(su => su.TrackingEnabled).HasDefaultValue(true);
        builder.Property(su => su.ClickLimit).HasDefaultValue(-1);
        builder.Property(e => e.TotalClicks).HasDefaultValue(0);
        builder.Property(su => su.IsPasswordProtected).HasDefaultValue(false);
        builder.Property(su => su.PasswordHash).HasMaxLength(256);
        builder.Property(su => su.IsPrivate).HasDefaultValue(false);
        builder.Property(su => su.ExpiresAt).HasColumnType("datetime2(0)");
        builder.Property(su => su.Title).HasMaxLength(50).IsUnicode();
        builder.Property(su => su.Description).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()").HasColumnType("datetime2(0)");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()").HasColumnType("datetime2(0)");

        builder.HasIndex(s => s.ShortCode).IsUnique();
        builder.HasIndex(s => s.OrganizationId);
        builder.HasIndex(s => s.CreatedByMemberId);
        builder.HasIndex(s => new { s.IsActive, s.ExpiresAt });
        builder.HasIndex(s => new { s.OwnerType, s.AnonymousSessionId });

        builder.HasIndex(s => new { s.UserId, s.CreatedAt })
            .HasDatabaseName("IX_ShortUrls_UserId_Analytics")
            .IsDescending(false, true) // UserId ASC, CreatedAt DESC
            .HasFilter("[UserId] IS NOT NULL")
            .IncludeProperties(s => new
            {
                s.Id,
                s.ShortCode,
                s.OriginalUrl,
                s.TotalClicks,
                s.IsActive
            });

        builder.HasIndex(s => new { s.TotalClicks, s.UserId })
            .HasDatabaseName("IX_ShortUrls_TotalClicks_Popular")
            .IsDescending(true, false) // TotalClicks DESC, UserId ASC
            .IncludeProperties(s => new
            {
                s.Id,
                s.ShortCode,
                s.OriginalUrl,
                s.CreatedAt,
                s.IsActive
            });

        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_ShortUrls_UserId_OwnerType1")
            .HasFilter("[UserId] IS NOT NULL AND [OwnerType] = 1")
            .IncludeProperties(s => new
            {
                s.Id,
                s.ShortCode,
                s.OriginalUrl,
                s.IsActive,
                s.ExpiresAt,
                s.CreatedAt,
                s.TotalClicks
            });

        // Relationships
        builder.HasMany(s => s.ClickEvents)
            .WithOne(c => c.ShortUrl)
            .HasForeignKey(c => c.ShortUrlId)
            .OnDelete(DeleteBehavior.Cascade);

        // Check constraint to ensure only one owner type
        builder.HasCheckConstraint("CK_ShortUrls_SingleOwner",
            """

                        (
                            [OwnerType] = 1
                            AND [UserId] IS NOT NULL
                            AND [OrganizationId] IS NULL
                            AND [CreatedByMemberId] IS NULL
                            AND [AnonymousSessionId] IS NULL
                        )
                        OR
                        (
                            [OwnerType] = 2
                            AND [UserId] IS NULL
                            AND [OrganizationId] IS NOT NULL
                            AND [CreatedByMemberId] IS NOT NULL
                            AND [AnonymousSessionId] IS NULL
                        )
                        OR
                        (
                            [OwnerType] = 3
                            AND [UserId] IS NULL
                            AND [OrganizationId] IS NULL
                            AND [CreatedByMemberId] IS NULL
                            AND [AnonymousSessionId] IS NOT NULL
                        )
                    
            """);
    }
}
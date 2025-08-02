using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Identity.Client;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

/// <summary>
/// Provides the entity configuration for the ShortUrl model in the database.
/// </summary>
/// <remarks>
/// This class implements the IEntityTypeConfiguration interface for the ShortUrl entity.
/// It defines configuration rules such as unique constraints, default values, and property behaviors.
/// </remarks>
/// <seealso cref="ShortUrl"/>
public class ShortUrlConfiguration: IEntityTypeConfiguration<ShortUrl>
{
    public void Configure(EntityTypeBuilder<ShortUrl> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(e => e.OriginalUrl).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.ShortCode).HasMaxLength(15).IsUnicode(false);
        builder.Property(su => su.OwnerType).HasConversion<byte>();
        builder.Property(s => s.AnonymousSessionId).HasMaxLength(128);
        builder.Property(s => s.AnonymousIpAddress).HasMaxLength(45);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(su => su.TrackingEnabled).HasDefaultValue(true);
        builder.Property(su => su.ClickLimit).HasDefaultValue(-1); // -1 means unlimited
        builder.Property(e => e.TotalClicks).HasDefaultValue(0);
        builder.Property(su => su.IsPasswordProtected).HasDefaultValue(false);
        builder.Property(su => su.PasswordHash).HasMaxLength(256);
        builder.Property(su => su.IsPrivate).HasDefaultValue(false);
        builder.Property(su => su.ExpiresAt).HasColumnType("datetime2(0)");
        builder.Property(su => su.Title).HasMaxLength(50).IsUnicode();
        builder.Property(su => su.Description).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()").HasColumnType("datetime2(0)");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()").HasColumnType("datetime2(0)");
        
        // Indexes
        builder.HasIndex(s => s.ShortCode).IsUnique();
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.OrganizationId);
        builder.HasIndex(s => s.CreatedByMemberId);
        builder.HasIndex(s => new { s.IsActive, s.ExpiresAt });
        builder.HasIndex(s => new { s.OwnerType, s.AnonymousSessionId });
        
        // Relationships
        builder.HasMany(s => s.ClickEvents)
            .WithOne(c => c.ShortUrl)
            .HasForeignKey(c => c.ShortUrlId)
            .OnDelete(DeleteBehavior.Cascade);
        
        
        // Check constraint to ensure only one owner type
        builder.HasCheckConstraint("CK_ShortUrls_SingleOwner", 
            @"(
            -- User owned: OwnerType = 1, UserId is set, OrganizationId is null, no anonymous fields
            ([OwnerType] = 1 AND [UserId] IS NOT NULL AND [OrganizationId] IS NULL AND [AnonymousSessionId] IS NULL AND [AnonymousIpAddress] IS NULL)
            OR
            -- Organization owned: OwnerType = 2, OrganizationId is set, UserId is null, no anonymous fields  
            ([OwnerType] = 2 AND [OrganizationId] IS NOT NULL AND [UserId] IS NULL AND [AnonymousSessionId] IS NULL AND [AnonymousIpAddress] IS NULL)
            OR
            -- Anonymous owned: OwnerType = 3, Both UserId and OrganizationId are null, anonymous fields can be set
            ([OwnerType] = 3 AND [UserId] IS NULL AND [OrganizationId] IS NULL)
        )");
        
        
        //Check constraint for CreatedByMemberId logic
        builder.HasCheckConstraint("CK_ShortUrls_CreatedByMember",
            @"(
            -- If OrganizationId is set, CreatedByMemberId should be set
            ([OrganizationId] IS NOT NULL AND [CreatedByMemberId] IS NOT NULL)
            OR
            -- If OrganizationId is null, CreatedByMemberId should be null
            ([OrganizationId] IS NULL AND [CreatedByMemberId] IS NULL)
        )");

    }
}


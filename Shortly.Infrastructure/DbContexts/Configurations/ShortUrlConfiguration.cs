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

        builder.Property(e => e.OriginalUrl)
            .HasMaxLength(2048);
            
        builder.Property(e => e.ShortCode)
            .HasMaxLength(50) 
            .IsUnicode(false) // ASCII only for better performance
            .IsFixedLength(false);
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");
        
        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");
        
        builder.Property(e => e.AccessCount)
            .HasDefaultValue(0);
        
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);
        
        builder.Property(e => e.ApiKey)
            .HasMaxLength(256)
            .IsUnicode(false);
        
        // Indexes
        builder.HasIndex(x => x.ShortCode)
            .IsUnique()
            .HasDatabaseName("IX_ShortUrls_ShortCode");
        
        // Composite index covers both single UserId and UserId + IsActive + CreatedAt queries  
         builder.HasIndex(x => new { x.UserId, x.IsActive, x.CreatedAt })
             .HasDatabaseName("IX_ShortUrls_UserId_IsActive_CreatedAt");
         
         // Foreign key relationship
         builder.HasOne(e => e.User)
             .WithMany()
             .HasForeignKey(e => e.UserId)
             .OnDelete(DeleteBehavior.SetNull);  // Their URLs become anonymous but still work
    }
   
}
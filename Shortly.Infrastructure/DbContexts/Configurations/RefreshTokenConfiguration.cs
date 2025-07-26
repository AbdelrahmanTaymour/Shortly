using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class RefreshTokenConfiguration: IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);
        
        
        // Properties configuration
        builder.Property(e => e.Token)
            .HasMaxLength(512)
            .IsUnicode(false);
            
        builder.Property(e => e.ExpiresAt)
            .HasColumnType("datetime2(0)");
            
        builder.Property(e => e.IsRevoked)
            .HasDefaultValue(false);
            
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");
        
        
        // Ignore computed properties
        builder.Ignore(e => e.IsExpired);
        builder.Ignore(e => e.IsActive);
        
        
        // Indexes for performance
        builder.HasIndex(x => x.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");
        
        
        // Foreign key relationship
        builder.HasOne(e => e.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
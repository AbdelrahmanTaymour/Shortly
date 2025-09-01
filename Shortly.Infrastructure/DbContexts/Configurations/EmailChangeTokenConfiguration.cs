using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class EmailChangeTokenConfiguration : IEntityTypeConfiguration<EmailChangeToken>
{
    public void Configure(EntityTypeBuilder<EmailChangeToken> builder)
    {
        // Primary key
        builder.HasKey(t => t.Id);
        
        // Properties configuration
        builder.Property(e => e.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.Token).IsRequired().HasMaxLength(256);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.OldEmail).IsRequired().HasMaxLength(256);
        builder.Property(e => e.NewEmail).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ExpiresAt).IsRequired().HasColumnType("datetime2");
        builder.Property(e => e.IsUsed).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.UsedAt).HasColumnType("datetime2");

        // Indexes
        builder.HasIndex(e => e.Token).IsUnique()
            .HasDatabaseName("IX_EmailChangeTokens_Token");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_EmailChangeTokens_UserId");

        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_EmailChangeTokens_ExpiresAt");

        builder.HasIndex(e => new { e.UserId, e.IsUsed })
            .HasDatabaseName("IX_EmailChangeTokens_UserId_IsUsed");

        // Foreign key relationship
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
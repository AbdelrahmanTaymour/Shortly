using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class UserActionTokenConfiguration : IEntityTypeConfiguration<UserActionToken>
{
    public void Configure(EntityTypeBuilder<UserActionToken> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(x => x.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TokenType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.Used).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

        // Foreign key relationship
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserActionTokens_UserId");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_UserActionTokens_TokenHash");

        builder.HasIndex(x => new { x.UserId, x.TokenType, x.Used })
            .HasDatabaseName("IX_UserActionTokens_UserId_TokenType_Used");
    }
}
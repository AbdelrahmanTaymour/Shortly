using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);
        
        builder.Property(e => e.Name)
            .HasMaxLength(100);
        
        builder.Property(e => e.Email)
            .HasMaxLength(256)
            .IsUnicode(false);
        
        builder.Property(e => e.Username)
            .HasMaxLength(50)
            .IsUnicode(false);
        
        builder.Property(e => e.Password)
            .HasMaxLength(256) // For hashed passwords
            .IsUnicode(false);
        
        builder.Property(e => e.SubscriptionPlan)
            .HasConversion<byte>()
            .HasDefaultValue(enSubscriptionPlan.Free);
        
        builder.Property(e => e.Role)
            .HasConversion<byte>()
            .HasDefaultValue(enUserRole.StandardUser);
        
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");
        
        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");
        
        // Indexes
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");
            
        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");
        
        builder.HasIndex(x => new { x.IsActive, x.SubscriptionPlan })
            .HasDatabaseName("IX_Users_IsActive_SubscriptionPlan");
    }
}
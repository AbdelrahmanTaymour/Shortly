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
        builder.HasKey(user => user.Id);
        
        builder.Property(user => user.Name)
            .HasMaxLength(100);
        
        builder.Property(user => user.Email)
            .HasMaxLength(256)
            .IsUnicode(false);
        
        builder.Property(user => user.Username)
            .HasMaxLength(50)
            .IsUnicode(false);
        
        builder.Property(user => user.PasswordHash)
            .HasMaxLength(256) // For hashed passwords
            .IsUnicode(false);
        
        builder.Property(user => user.SubscriptionPlan)
            .HasConversion<byte>()
            .HasDefaultValue(enSubscriptionPlan.Free);
        
        builder.Property(user => user.Role)
            .HasConversion<byte>()
            .HasDefaultValue(enUserRole.StandardUser);
        
        builder.Property(user => user.IsActive)
            .HasDefaultValue(true);

        builder.Property(user => user.IsDeleted)
            .HasDefaultValue(false);
        
        builder.Property(user => user.IsEmailConfirmed)
            .HasDefaultValue(false);
        
        builder.Property(user => user.ProfilePictureUrl)
            .HasMaxLength(512)
            .IsUnicode(false);

        builder.Property(user => user.LastLoginAt)
            .HasColumnType("datetime2(0)");
        
        builder.Property(user => user.TimeZone)
            .HasMaxLength(50)
            .HasDefaultValue("UTC");
        
        builder.Property(user => user.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");
        
        builder.Property(user => user.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");
        
        builder.Property(user => user.DeletedAt)
            .HasColumnType("datetime2(0)");
        
        
        // Usage tracking for subscription limits
        
        builder.Property(user => user.MonthlyLinksCreated)
            .HasDefaultValue(0);

        builder.Property(user => user.TotalLinksCreated)
            .HasDefaultValue(0);

        builder.Property(user => user.MonthlyResetDate)
            .HasColumnType("datetime2(0)")
            .HasDefaultValueSql("DATEADD(MONTH, 1, GETUTCDATE())");
        
        // Security
        builder.Property(user => user.TwoFactorEnabled)
            .HasDefaultValue(false);

        builder.Property(user => user.TwoFactorSecret)
            .HasMaxLength(256)
            .IsUnicode(false);

        builder.Property(user => user.FailedLoginAttempts)
            .HasDefaultValue(0);

        builder.Property(user => user.LockedUntil)
            .HasColumnType("datetime2(0)");
        
        // Indexes
        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");
            
        builder.HasIndex(user => user.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");
        
        builder.HasIndex(user => new { user.IsActive, user.SubscriptionPlan })
            .HasDatabaseName("IX_Users_IsActive_SubscriptionPlan");
    }
}
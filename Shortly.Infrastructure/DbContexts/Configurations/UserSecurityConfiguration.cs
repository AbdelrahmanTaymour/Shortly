using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class UserSecurityConfiguration : IEntityTypeConfiguration<UserSecurity>
{
    public void Configure(EntityTypeBuilder<UserSecurity> builder)
    {
        // Primary Key
        builder.HasKey(us => us.UserId);

        // Properties configuration
        builder.Property(us => us.FailedLoginAttempts).HasDefaultValue(0);
        builder.Property(us => us.TwoFactorEnabled).HasDefaultValue(false);
        builder.Property(us => us.TwoFactorSecret).HasMaxLength(150);
        builder.Property(us => us.PasswordResetToken).HasMaxLength(256);
        builder.Property(us => us.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        // Foreign Key
        builder.HasOne(us => us.User)
            .WithOne(u => u.UserSecurity)
            .HasForeignKey<UserSecurity>(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index
        builder.HasIndex(us => us.PasswordResetToken);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.Configurations;

public class UserSecurityConfiguration : IEntityTypeConfiguration<UserSecurity>
{
    public void Configure(EntityTypeBuilder<UserSecurity> builder)
    {
        // Primary Key
        builder.HasKey(us => us.UserId);

        // Properties configuration
        builder.Property(us => us.FailedLoginAttempts).HasDefaultValue(0);
        builder.Property(us => us.LockoutReason).HasMaxLength(100);
        builder.Property(us => us.TwoFactorEnabled).HasDefaultValue(false);
        builder.Property(us => us.TwoFactorSecret).HasMaxLength(150);
        builder.Property(us => us.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Configurations;

public sealed class UserSecurityConfiguration : IEntityTypeConfiguration<UserSecurity>
{
    public void Configure(EntityTypeBuilder<UserSecurity> builder)
    {
        builder.ToTable("user_security");

        builder.HasKey(us => us.UserId);

        builder.Property(us => us.FailedLoginAttempts).HasDefaultValue(0);
        builder.Property(us => us.LockoutReason).HasMaxLength(100);
        builder.Property(us => us.TwoFactorEnabled).HasDefaultValue(false);
        builder.Property(us => us.TwoFactorSecret).HasMaxLength(150);
        builder.Property(us => us.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.Property<uint>("Version").IsRowVersion();
    }
}
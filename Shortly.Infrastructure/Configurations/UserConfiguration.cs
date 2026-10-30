using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(320).IsUnicode(false);
        builder.Property(u => u.Username).IsRequired().HasMaxLength(50).IsUnicode(false);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256).IsUnicode(false);
        builder.Property(u => u.SubscriptionPlanId).HasConversion<byte>();
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.IsEmailConfirmed).HasDefaultValue(false);
        builder.Property(u => u.IsDeleted).HasDefaultValue(false);
        builder.Property(u => u.IsOAuthUser).HasDefaultValue(false);
        builder.Property(u => u.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(u => u.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(u => u.DeletedBy).HasColumnType("uuid");

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ux_users_email");
        builder.HasIndex(u => u.Username).IsUnique().HasDatabaseName("ux_users_username");
        builder.HasIndex(u => new { u.IsDeleted, u.IsActive }).HasDatabaseName("ix_users_is_deleted_is_active");

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(u => u.SubscriptionPlanId)
            .HasConstraintName("fk_users_subscription_plans")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(u => u.DeletedBy)
            .HasConstraintName("fk_users_users_deleted_by")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Profile)
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.UserId)
            .HasConstraintName("fk_user_profile_users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Security)
            .WithOne()
            .HasForeignKey<UserSecurity>(s => s.UserId)
            .HasConstraintName("fk_user_security_users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Usage)
            .WithOne()
            .HasForeignKey<UserUsage>(uu => uu.UserId)
            .HasConstraintName("fk_user_usage_users")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
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
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320)
            .IsUnicode(false);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50)
            .IsUnicode(false);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256)
            .IsUnicode(false);

        builder.Property(u => u.SubscriptionPlanId)
            .HasConversion<byte>();

        builder.Property(u => u.Permissions)
            .HasDefaultValue(enPermissions.BasicUser);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.IsEmailConfirmed)
            .HasDefaultValue(false);

        builder.Property(u => u.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(u => u.IsOAuthUser)
            .HasDefaultValue(false);

        builder.Property(u => u.LastLoginAt)
            .HasColumnType("datetime2(0)");

        builder.Property(u => u.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");

        builder.Property(u => u.DeletedAt)
            .HasColumnType("datetime2(0)");

        builder.Property(u => u.DeletedBy)
            .HasColumnType("uniqueidentifier");

        // Indexes
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.IsDeleted);
        builder.HasIndex(u => new { u.IsDeleted, u.IsActive });

        // Relationships
        builder.HasOne(u => u.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(u => u.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.UserSecurity)
            .WithOne(s => s.User)
            .HasForeignKey<UserSecurity>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.UserUsage)
            .WithOne(uu => uu.User)
            .HasForeignKey<UserUsage>(uu => uu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.OwnedShortUrls)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.AuditLogs)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.OrganizationMemberships)
            .WithOne(om => om.User)
            .HasForeignKey(om => om.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
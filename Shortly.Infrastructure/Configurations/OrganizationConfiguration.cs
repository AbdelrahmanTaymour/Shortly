using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).HasMaxLength(100);
        builder.Property(o => o.Description).HasMaxLength(500);
        builder.Property(o => o.Website).HasMaxLength(500).IsUnicode(false);
        builder.Property(o => o.LogoUrl).HasMaxLength(500);
        builder.Property(o => o.MemberLimit).HasDefaultValue(10);
        builder.Property(o => o.IsActive).HasDefaultValue(false);
        builder.Property(o => o.IsSubscribed).HasDefaultValue(false);
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(o => o.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(o => o.SubscriptionPlanId).HasConversion<byte>();

        builder.HasIndex(o => o.IsActive)
            .HasDatabaseName("ix_organizations_is_active");

        builder.HasIndex(o => new { o.IsActive, o.DeletedAt })
            .HasDatabaseName("ix_organizations_is_active_deleted_at");

        builder.HasIndex(o => new { o.OwnerId, o.Name })
            .IsUnique()
            .HasDatabaseName("ux_organizations_owner_id_name");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(o => o.OwnerId)
            .HasConstraintName("fk_organizations_users")
            .OnDelete(DeleteBehavior.Restrict); // deleting a user must not cascade-delete every org they own

        builder.HasOne(o => o.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(o => o.SubscriptionPlanId)
            .HasConstraintName("fk_organizations_subscription_plans")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.OrganizationUsage)
            .WithOne(ou => ou.Organization)
            .HasForeignKey<OrganizationUsage>(ou => ou.OrganizationId)
            .HasConstraintName("fk_organization_usage_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Teams)
            .WithOne(t => t.Organization)
            .HasForeignKey(t => t.OrganizationId)
            .HasConstraintName("fk_organization_teams_organizations")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.AuditLogs)
            .WithOne()
            .HasForeignKey(a => a.OrganizationId)
            .HasConstraintName("fk_organization_audit_logs_organizations")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
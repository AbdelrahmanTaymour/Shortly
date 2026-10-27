using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(e => e.Name).HasMaxLength(100);
        builder.Property(o => o.Description).HasMaxLength(500);
        builder.Property(o => o.Website).HasMaxLength(500).IsUnicode(false);
        builder.Property(o => o.LogoUrl).HasMaxLength(500);
        builder.Property(o => o.MemberLimit).HasDefaultValue(10);
        builder.Property(o => o.IsActive).HasDefaultValue(false);
        builder.Property(o => o.IsSubscribed).HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(o => o.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(o => o.DeletedAt);
        builder.Property(user => user.SubscriptionPlanId).HasConversion<byte>();

        // Indexes
        builder.HasIndex(o => o.OwnerId);
        builder.HasIndex(o => o.IsActive);
        builder.HasIndex(o => new { o.IsActive, o.DeletedAt });
        builder.HasIndex(o => new { o.OwnerId, o.Name }).IsUnique();

        // Relationships
        builder.HasOne(o => o.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(u => u.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(o => o.OrganizationUsage)
            .WithOne(ou => ou.Organization)
            .HasForeignKey<OrganizationUsage>(ou => ou.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Teams)
            .WithOne(t => t.Organization)
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(o => o.ShortUrls)
            .WithOne(s => s.Organization)
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(o => o.AuditLogs)
            .WithOne(a => a.Organization)
            .HasForeignKey(a => a.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(e => e.RoleId)
            .HasConversion<byte>()
            .HasDefaultValue(enUserRole.Member);

        builder.Property(e => e.CustomPermissions)
            .HasConversion<long>().HasDefaultValue(enPermissions.None);

        builder.Property(e => e.JoinedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(om => new { om.OrganizationId, om.UserId }).IsUnique();
        builder.HasIndex(om => om.UserId);
        builder.HasIndex(om => new { om.OrganizationId, om.IsActive });

        // Relationships
        builder.HasOne(om => om.Role)
            .WithMany()
            .HasForeignKey(om => om.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(om => om.CreatedShortUrls)
            .WithOne(s => s.CreatedBy)
            .HasForeignKey(s => s.CreatedByMemberId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(om => om.JoinedTeams)
            .WithOne(tm => tm.Member)
            .HasForeignKey(tm => tm.MemberId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
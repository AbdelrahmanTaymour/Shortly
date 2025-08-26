using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class OrganizationInvitationConfiguration : IEntityTypeConfiguration<OrganizationInvitation>
{
    public void Configure(EntityTypeBuilder<OrganizationInvitation> builder)
    {
        // Primary key
        builder.HasKey(oi => oi.Id);

        // Properties configuration
        builder.Property(oi => oi.InvitedUserEmail).HasMaxLength(320);
        builder.Property(oi => oi.InvitedUserRoleId).HasConversion<byte>();
        builder.Property(oi => oi.InvitedUserPermissions).HasConversion<long>();
        builder.Property(oi => oi.Status).HasConversion<byte>().HasDefaultValue(enInvitationStatus.Pending);
        builder.Property(oi => oi.RegisteredAt).HasColumnType("datetime2(0)");
        builder.Property(oi => oi.ExpiresAt)
            .HasColumnType("datetime2(0)")
            .HasDefaultValueSql("DATEADD(day, 5, GETUTCDATE())"); // Set expiry in database
        builder.Property(oi => oi.CreatedAt).HasDefaultValueSql("GETUTCDATE()").HasColumnType("datetime2(0)");

        // Computed column for IsExpired (When ExpiresAt date passed or the invitation status is Failure(3), Registered(5), or Rejected(6))
        builder.Property(oi => oi.IsExpired)
            .HasComputedColumnSql("CASE WHEN [ExpiresAt] < GETUTCDATE() OR [Status] IN (3,5,6) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END");

        // Indexes
        builder.HasIndex(oi => new { oi.OrganizationId, oi.InvitedUserEmail, oi.Status });
        builder.HasIndex(oi => oi.InvitedBy);

        // Relationships
        builder.HasOne(oi => oi.InvitedByMember)
            .WithMany()
            .HasForeignKey(oi => oi.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(om => om.InvitedUserRole)
            .WithMany()
            .HasForeignKey(om => om.InvitedUserRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
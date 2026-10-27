using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.Configurations;

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
        builder.Property(oi => oi.Status).HasConversion<byte>()
            .HasDefaultValue(enInvitationStatus.Pending);
        builder.Property(oi => oi.RegisteredAt);
        builder.Property(oi => oi.ExpiresAt)
            .HasDefaultValueSql("NOW() + INTERVAL '5 days'");
        builder.Property(oi => oi.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Ignore database generated column; compute in C# entity instead
        builder.Ignore(oi => oi.IsExpired);

        // Indexes
        builder.HasIndex(oi => new { oi.OrganizationId, oi.InvitedUserEmail, oi.Status });
        builder.HasIndex(oi => oi.InvitedBy);

        // Relationships
        builder.HasOne(oi => oi.InvitedByMember)
            .WithMany()
            .HasForeignKey(oi => oi.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
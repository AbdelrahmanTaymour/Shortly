using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.Configurations;

public sealed class OrganizationInvitationConfiguration : IEntityTypeConfiguration<OrganizationInvitation>
{
    public void Configure(EntityTypeBuilder<OrganizationInvitation> builder)
    {
        builder.ToTable("organization_invitations");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.InvitedUserEmail).HasMaxLength(320).IsUnicode(false);
        builder.Property(oi => oi.InvitedUserRoleId).HasConversion<byte>();
        builder.Property(oi => oi.InvitedUserPermissions).HasConversion<long>();
        builder.Property(oi => oi.Status)
            .HasConversion<byte>()
            .HasDefaultValue(enInvitationStatus.Pending);
        builder.Property(oi => oi.ExpiresAt).HasDefaultValueSql("NOW() + INTERVAL '5 days'");
        builder.Property(oi => oi.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Ignore(oi => oi.IsExpired);

        builder.HasIndex(oi => new { oi.OrganizationId, oi.InvitedUserEmail, oi.Status })
            .HasDatabaseName("ix_organization_invitations_organization_id_email_status");

        builder.HasIndex(oi => oi.InvitedBy)
            .HasDatabaseName("ix_organization_invitations_invited_by");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(oi => oi.OrganizationId)
            .HasConstraintName("fk_organization_invitations_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.InvitedByMember)
            .WithMany()
            .HasForeignKey(oi => oi.InvitedBy)
            .HasConstraintName("fk_organization_invitations_organization_members")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Enums;
using Shortly.Domain.Invitations;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Configurations;

public sealed class OrganizationInvitationConfiguration : IEntityTypeConfiguration<OrganizationInvitation>
{
    public void Configure(EntityTypeBuilder<OrganizationInvitation> builder)
    {
        builder.ToTable("organization_invitations");
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.InvitedUserEmail).HasMaxLength(320).IsUnicode(false);
        builder.Property(oi => oi.InvitedUserRole).HasConversion<byte>();
        builder.Property(oi => oi.InvitedUserPermissions).HasConversion<long>();
        builder.Property(oi => oi.TokenHash).HasMaxLength(128).IsUnicode(false);
        builder.Property(oi => oi.Status).HasConversion<byte>().HasDefaultValue(enInvitationStatus.Pending);
        builder.Property(oi => oi.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(oi => oi.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_organization_invitations_token_hash");

        builder.HasIndex(oi => new { oi.OrganizationId, oi.InvitedUserEmail, oi.Status })
            .HasDatabaseName("ix_organization_invitations_organization_id_email_status");

        builder.HasIndex(oi => oi.InvitedBy)
            .HasDatabaseName("ix_organization_invitations_invited_by");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(oi => oi.OrganizationId)
            .HasConstraintName("fk_organization_invitations_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(oi => oi.InvitedBy)
            .HasConstraintName("fk_organization_invitations_users")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
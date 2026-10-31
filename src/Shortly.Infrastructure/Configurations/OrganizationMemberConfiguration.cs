using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Configurations;

public sealed class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("organization_members");
        builder.HasKey(om => om.Id);

        builder.Property(om => om.Role).HasConversion<byte>().HasDefaultValue(MemberRole.Member);
        builder.Property(om => om.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(om => om.IsActive).HasDefaultValue(true);

        builder.HasIndex(om => new { om.OrganizationId, om.UserId })
            .IsUnique()
            .HasDatabaseName("ux_organization_members_organization_id_user_id");

        builder.HasIndex(om => om.UserId)
            .HasDatabaseName("ix_organization_members_user_id");

        builder.HasIndex(om => new { om.OrganizationId, om.IsActive })
            .HasDatabaseName("ix_organization_members_organization_id_is_active");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(om => om.OrganizationId)
            .HasConstraintName("fk_organization_members_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(om => om.UserId)
            .HasConstraintName("fk_organization_members_users")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
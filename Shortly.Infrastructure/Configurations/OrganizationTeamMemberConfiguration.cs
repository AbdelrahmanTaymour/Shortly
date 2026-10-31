using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Infrastructure.Configurations;

public sealed class OrganizationTeamMemberConfiguration : IEntityTypeConfiguration<OrganizationTeamMember>
{
    public void Configure(EntityTypeBuilder<OrganizationTeamMember> builder)
    {
        builder.ToTable("team_members");
        builder.HasKey(otm => otm.Id);

        builder.Property(otm => otm.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(otm => new { otm.TeamId, otm.MemberId })
            .IsUnique()
            .HasDatabaseName("ux_team_members_team_id_member_id");

        builder.HasIndex(otm => otm.MemberId)
            .HasDatabaseName("ix_team_members_member_id");

        builder.HasOne<OrganizationTeam>()
            .WithMany()
            .HasForeignKey(otm => otm.TeamId)
            .HasConstraintName("fk_team_members_organization_teams")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(otm => otm.MemberId)
            .HasConstraintName("fk_team_members_organization_members")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
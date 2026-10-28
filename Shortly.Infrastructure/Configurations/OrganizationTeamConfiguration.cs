using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.Configurations;

public sealed class OrganizationTeamConfiguration : IEntityTypeConfiguration<OrganizationTeam>
{
    public void Configure(EntityTypeBuilder<OrganizationTeam> builder)
    {
        builder.ToTable("organization_teams");

        builder.HasKey(ot => ot.Id);

        builder.Property(ot => ot.Name).HasMaxLength(100);
        builder.Property(ot => ot.Description).HasMaxLength(500);
        builder.Property(ot => ot.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(ot => new { ot.OrganizationId, ot.Name })
            .IsUnique()
            .HasDatabaseName("ux_organization_teams_organization_id_name");

        builder.HasIndex(ot => ot.TeamManagerId)
            .HasDatabaseName("ix_organization_teams_team_manager_id");

        builder.HasOne(ot => ot.TeamManager)
            .WithMany()
            .HasForeignKey(ot => ot.TeamManagerId)
            .HasConstraintName("fk_organization_teams_organization_members")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(ot => ot.TeamMembers)
            .WithOne(tm => tm.Team)
            .HasForeignKey(tm => tm.TeamId)
            .HasConstraintName("fk_team_members_organization_teams")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
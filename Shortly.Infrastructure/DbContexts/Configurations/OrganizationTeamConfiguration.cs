using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class OrganizationTeamConfiguration:IEntityTypeConfiguration<OrganizationTeam>
{
    public void Configure(EntityTypeBuilder<OrganizationTeam> builder)
    {
        // Primary key
        builder.HasKey(ot => ot.Id);
        
        // Properties configuration
        builder.Property(ot => ot.Name).HasMaxLength(100);
        builder.Property(ot => ot.Description).HasMaxLength(500);
        builder.Property(ot => ot.CreatedAt).HasDefaultValueSql("GETUTCDATE()").HasColumnType("datetime2(0)");
        
        // Indexes
        builder.HasIndex(ot => new { ot.OrganizationId, ot.Name }).IsUnique();
        builder.HasIndex(ot => ot.TeamManagerId);

        // Relationships
        builder.HasOne(ot => ot.TeamManager)
            .WithMany()
            .HasForeignKey(ot => ot.TeamManagerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(ot => ot.TeamMembers)
            .WithOne(tm => tm.Team)
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
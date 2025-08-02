using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class OrganizationTeamMemberConfiguration:IEntityTypeConfiguration<OrganizationTeamMember>
{
    public void Configure(EntityTypeBuilder<OrganizationTeamMember> builder)
    {
        // Primary key
        builder.HasKey(otm => otm.Id);
        
        // Properties configuration
        builder.Property(otm => otm.JoinedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");
        
        // Indexes
        builder.HasIndex(otm => new { otm.TeamId, otm.MemberId }).IsUnique();
        builder.HasIndex(otm => otm.MemberId);

    }
}
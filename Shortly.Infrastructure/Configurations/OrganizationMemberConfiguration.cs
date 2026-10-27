using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Members;

namespace Shortly.Infrastructure.Configurations;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(e => e.Role)
            .HasConversion<byte>()
            .HasDefaultValue(MemberRole.Member);
        
        builder.Property(e => e.JoinedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(om => new { om.OrganizationId, om.UserId }).IsUnique();
        builder.HasIndex(om => om.UserId);
        builder.HasIndex(om => new { om.OrganizationId, om.IsActive });
    }
}
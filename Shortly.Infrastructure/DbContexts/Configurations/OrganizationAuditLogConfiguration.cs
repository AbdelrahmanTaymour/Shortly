using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class OrganizationAuditLogConfiguration : IEntityTypeConfiguration<OrganizationAuditLog>
{
    public void Configure(EntityTypeBuilder<OrganizationAuditLog> builder)
    {
        // Primary key
        builder.HasKey(oal => oal.Id);

        // Properties configuration
        builder.Property(oal => oal.Event).HasMaxLength(100).IsUnicode(false);
        builder.Property(oal => oal.TargetEntity).HasMaxLength(100).IsUnicode(false);
        builder.Property(oal => oal.TargetId).HasMaxLength(50).IsUnicode(false);
        builder.Property(oal => oal.Details).HasMaxLength(1000).IsUnicode();
        builder.Property(oal => oal.TimeStamp).HasDefaultValueSql("GETUTCDATE()").HasColumnType("datetime2(0)");

        // Indexes
        builder.HasIndex(oal => oal.OrganizationId);
        builder.HasIndex(oal => oal.ActorId);
        builder.HasIndex(oal => new { oal.OrganizationId, oal.TimeStamp });

        // Relationships
        builder.HasOne(oal => oal.Actor)
            .WithMany()
            .HasForeignKey(oal => oal.ActorId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
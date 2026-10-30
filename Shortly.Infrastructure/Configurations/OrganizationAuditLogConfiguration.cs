using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Infrastructure.Configurations;

public sealed class OrganizationAuditLogConfiguration : IEntityTypeConfiguration<OrganizationAuditLog>
{
    public void Configure(EntityTypeBuilder<OrganizationAuditLog> builder)
    {
        builder.ToTable("organization_audit_logs");

        builder.HasKey(oal => oal.Id);

        builder.Property(oal => oal.Event).HasMaxLength(100).IsUnicode(false);
        builder.Property(oal => oal.TargetEntity).HasMaxLength(100).IsUnicode(false);
        builder.Property(oal => oal.TargetId).HasMaxLength(50).IsUnicode(false);
        builder.Property(oal => oal.Details).HasMaxLength(1000);
        builder.Property(oal => oal.TimeStamp).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(oal => oal.ActorId)
            .HasDatabaseName("ix_organization_audit_logs_actor_id");

        builder.HasIndex(oal => new { oal.OrganizationId, oal.TimeStamp })
            .HasDatabaseName("ix_organization_audit_logs_organization_id_timestamp");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(oal => oal.OrganizationId)
            .HasConstraintName("fk_organization_audit_logs_organizations")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(oal => oal.ActorId)
            .HasConstraintName("fk_organization_audit_logs_organization_members")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
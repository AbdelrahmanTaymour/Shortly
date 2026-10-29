using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Configurations;

public sealed class UserAuditLogConfiguration : IEntityTypeConfiguration<UserAuditLog>
{
    public void Configure(EntityTypeBuilder<UserAuditLog> builder)
    {
        builder.ToTable("user_audit_logs");

        builder.HasKey(ual => ual.Id);

        builder.Property(ual => ual.Action).HasMaxLength(100).IsUnicode(false);
        builder.Property(ual => ual.Details).HasMaxLength(1000);
        builder.Property(ual => ual.TimeStamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(ual => ual.IpAddress).HasMaxLength(45).IsUnicode(false);
        builder.Property(ual => ual.UserAgent).HasMaxLength(500);

        builder.HasIndex(ual => ual.TimeStamp)
            .HasDatabaseName("ix_user_audit_logs_timestamp");

        builder.HasIndex(ual => new { ual.UserId, ual.TimeStamp })
            .HasDatabaseName("ix_user_audit_logs_user_id_timestamp");

        builder.HasOne<User>()
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(ual => ual.UserId)
            .HasConstraintName("fk_user_audit_logs_users")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
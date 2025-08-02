using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class UserAuditLogConfiguration:IEntityTypeConfiguration<UserAuditLog>
{
    public void Configure(EntityTypeBuilder<UserAuditLog> builder)
    {
        // Primary Key
        builder.HasKey(ual => ual.Id);
        
        builder.Property(ual => ual.Action).HasMaxLength(100).IsUnicode(false);
        builder.Property(ual => ual.Details).HasMaxLength(1000);
        builder.Property(ual => ual.TimeStamp).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(ual => ual.IpAddress).HasMaxLength(45).IsUnicode(false);
        builder.Property(ual => ual.UserAgent).HasMaxLength(500);
        
        // Indexes
        builder.HasIndex(ua => ua.UserId);
        builder.HasIndex(ua => ua.TimeStamp);
        builder.HasIndex(ua => new { ua.UserId, ua.TimeStamp });

    }
}
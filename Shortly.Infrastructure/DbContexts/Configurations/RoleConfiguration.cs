using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Primary Key
        builder.HasKey(r => r.Id);
        
        // Properties configuration
        builder.Property(r => r.RoleName).HasMaxLength(30).IsUnicode(false);
        builder.Property(r => r.Description).HasMaxLength(200).IsUnicode();
        
        // Index
        builder.HasIndex(r => r.RoleName).IsUnique();
    }
}
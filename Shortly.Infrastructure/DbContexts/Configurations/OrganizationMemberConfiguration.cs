using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class OrganizationMemberConfiguration: IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);
        
        // Properties configuration
            
        builder.Property(e => e.Role)
            .HasConversion<byte>()
            .HasDefaultValue(enUserRole.StandardUser);
            
        builder.Property(e => e.CustomPermissions)
            .HasConversion<int>()
            .HasDefaultValue(enPermissions.None);
            
        builder.Property(e => e.JoinedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");
            
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);
        
        // Indexes for performance
        builder.HasIndex(x => new { x.OrganizationId, x.UserId })
            .IsUnique()
            .HasDatabaseName("IX_OrganizationMembers_OrganizationId_UserId");
        
        
        // Foreign key relationships
        builder.HasOne(e => e.Organization)
            .WithMany(e => e.Members)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
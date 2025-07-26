using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class OrganizationConfiguration: IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);
        
        
        // Properties configuration
        builder.Property(e => e.Name)
            .HasMaxLength(50);
            
        builder.Property(e => e.SubscriptionPlan)
            .HasConversion<byte>()
            .HasDefaultValue(enSubscriptionPlan.Enterprise);
            
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .HasColumnType("datetime2(0)");
        
        
        // Foreign key relationships
        builder.HasOne(e => e.Owner)
            .WithMany(u => u.OwnedOrganizations)
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete
            
        builder.HasMany(e => e.Members)
            .WithOne(e => e.Organization)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
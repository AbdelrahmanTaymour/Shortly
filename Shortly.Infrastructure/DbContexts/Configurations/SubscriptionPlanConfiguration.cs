using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        // Primary Key
        builder.HasKey(sp => sp.Id);

        // Properties configuration
        builder.Property(sp => sp.Id).HasConversion<byte>().ValueGeneratedNever();
        builder.Property(sp => sp.Name).HasMaxLength(50);
        builder.Property(sp => sp.Description).HasMaxLength(500);
        builder.Property(sp => sp.Price).HasColumnType("decimal(10,2)");

        // Index
        builder.HasIndex(sp => sp.Name).IsUnique();
    }
}
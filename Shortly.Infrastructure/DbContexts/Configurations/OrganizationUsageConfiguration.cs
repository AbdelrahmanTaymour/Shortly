using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class OrganizationUsageConfiguration : IEntityTypeConfiguration<OrganizationUsage>
{
    public void Configure(EntityTypeBuilder<OrganizationUsage> builder)
    {
        // Primary Key
        builder.HasKey(ou => ou.OrganizationId);

        // Properties configuration
        builder.Property(ou => ou.MonthlyLinksCreated).HasDefaultValue(0);
        builder.Property(ou => ou.MonthlyQrCodesCreated).HasDefaultValue(0);
        builder.Property(ou => ou.TotalLinksCreated).HasDefaultValue(0);
        builder.Property(ou => ou.TotalQrCodesCreated).HasDefaultValue(0);
        builder.Property(ou => ou.MonthlyResetDate).HasDefaultValueSql("DATEADD(month, 1, GETUTCDATE())");
    }
}
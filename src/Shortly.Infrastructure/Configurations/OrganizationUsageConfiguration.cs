using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Organizations;

namespace Shortly.Infrastructure.Configurations;

public sealed class OrganizationUsageConfiguration : IEntityTypeConfiguration<OrganizationUsage>
{
    public void Configure(EntityTypeBuilder<OrganizationUsage> builder)
    {
        builder.ToTable("organization_usage");

        builder.HasKey(ou => ou.OrganizationId);

        builder.Property(ou => ou.MonthlyLinksCreated).HasDefaultValue(0);
        builder.Property(ou => ou.MonthlyQrCodesCreated).HasDefaultValue(0);
        builder.Property(ou => ou.TotalLinksCreated).HasDefaultValue(0);
        builder.Property(ou => ou.TotalQrCodesCreated).HasDefaultValue(0);
        builder.Property(ou => ou.MonthlyResetDate).HasDefaultValueSql("CURRENT_TIMESTAMP + INTERVAL '1 month'");
        
        builder.Property<uint>("Version").IsRowVersion();
    }
}
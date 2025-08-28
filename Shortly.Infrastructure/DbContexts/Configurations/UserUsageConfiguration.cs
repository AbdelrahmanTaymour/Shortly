using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;

namespace Shortly.Infrastructure.DbContexts.Configurations;

public class UserUsageConfiguration : IEntityTypeConfiguration<UserUsage>
{
    public void Configure(EntityTypeBuilder<UserUsage> builder)
    {
        // Primary Key
        builder.HasKey(uu => uu.UserId);

        // Properties configuration
        builder.Property(uu => uu.MonthlyLinksCreated).HasDefaultValue(0);
        builder.Property(uu => uu.MonthlyQrCodesCreated).HasDefaultValue(0);
        builder.Property(uu => uu.TotalLinksCreated).HasDefaultValue(0);
        builder.Property(uu => uu.TotalQrCodesCreated).HasDefaultValue(0);
        builder.Property(uu => uu.MonthlyResetDate).HasDefaultValueSql("DATEADD(month, 1, GETUTCDATE())");
    }
}
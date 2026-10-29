using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Configurations;

public sealed class UserUsageConfiguration : IEntityTypeConfiguration<UserUsage>
{
    public void Configure(EntityTypeBuilder<UserUsage> builder)
    {
        builder.ToTable("user_usage");

        builder.HasKey(uu => uu.UserId);

        builder.Property(uu => uu.MonthlyLinksCreated).HasDefaultValue(0);
        builder.Property(uu => uu.MonthlyQrCodesCreated).HasDefaultValue(0);
        builder.Property(uu => uu.TotalLinksCreated).HasDefaultValue(0);
        builder.Property(uu => uu.TotalQrCodesCreated).HasDefaultValue(0);
        builder.Property(uu => uu.MonthlyResetDate).HasDefaultValueSql("CURRENT_TIMESTAMP + INTERVAL '1 month'");
    }
}
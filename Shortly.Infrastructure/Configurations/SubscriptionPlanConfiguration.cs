using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Enums;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Infrastructure.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Id).HasConversion<byte>().ValueGeneratedNever();

        builder.Property(sp => sp.Name).HasMaxLength(50);
        builder.Property(sp => sp.Description).HasMaxLength(500);
        builder.Property(sp => sp.Price).HasColumnType("decimal(10,2)");

        builder.Ignore(sp => sp.HasUnlimitedLinks);
        builder.Ignore(sp => sp.HasUnlimitedQrCodes);
        builder.Ignore(sp => sp.HasUnlimitedRetention);

        builder.HasIndex(sp => sp.Name)
            .IsUnique()
            .HasDatabaseName("ux_subscription_plans_name");

        builder.HasData(
            new SubscriptionPlan(enSubscriptionPlan.Free, "Free", "Try it out for free", price: 0,
                maxLinksPerMonth: 5, maxQrCodesPerMonth: 2, clickDataRetentionDays: 0,
                linkAnalysis: false, bulkCreation: false, linkProtection: false,
                customShortCode: false, campaignTracking: false, geoDeviceTracking: false),

            new SubscriptionPlan(enSubscriptionPlan.Starter, "Starter", "Unlock powerful data", price: 10,
                maxLinksPerMonth: 100, maxQrCodesPerMonth: 5, clickDataRetentionDays: 30,
                linkAnalysis: true, bulkCreation: false, linkProtection: false,
                customShortCode: true, campaignTracking: false, geoDeviceTracking: false),

            new SubscriptionPlan(enSubscriptionPlan.Professional, "Professional", "For growing teams", price: 50,
                maxLinksPerMonth: 500, maxQrCodesPerMonth: 10, clickDataRetentionDays: 90,
                linkAnalysis: true, bulkCreation: true, linkProtection: true,
                customShortCode: true, campaignTracking: true, geoDeviceTracking: false),

            new SubscriptionPlan(enSubscriptionPlan.Enterprise, "Enterprise", "Unlimited everything", price: 200,
                maxLinksPerMonth: -1, maxQrCodesPerMonth: -1, clickDataRetentionDays: -1,
                linkAnalysis: true, bulkCreation: true, linkProtection: true,
                customShortCode: true, campaignTracking: true, geoDeviceTracking: true)
        );
    }
}
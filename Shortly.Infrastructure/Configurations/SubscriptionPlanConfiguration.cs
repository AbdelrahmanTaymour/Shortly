using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<Domain.Entities.SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Id).HasConversion<byte>().ValueGeneratedNever();

        builder.Property(sp => sp.Name).HasMaxLength(50);
        builder.Property(sp => sp.Description).HasMaxLength(500);
        builder.Property(sp => sp.Price).HasColumnType("decimal(10,2)");

        builder.HasIndex(sp => sp.Name)
            .IsUnique()
            .HasDatabaseName("ux_subscription_plans_name");

        builder.HasData(
            new Domain.Entities.SubscriptionPlan
            {
                Id = enSubscriptionPlan.Free, Name = "Free", Description = "Try it out for free",
                Price = 0, MaxQrCodesPerMonth = 2, MaxLinksPerMonth = 5, ClickDataRetentionDays = 0,
                LinkAnalysis = false, BulkCreation = false, LinkProtection = false,
                CustomShortCode = false, CampaignTracking = false, GeoDeviceTracking = false
            },
            new Domain.Entities.SubscriptionPlan
            {
                Id = enSubscriptionPlan.Starter, Name = "Starter", Description = "Unlock powerful data",
                Price = 10, MaxQrCodesPerMonth = 5, MaxLinksPerMonth = 100, ClickDataRetentionDays = 30,
                LinkAnalysis = true, CustomShortCode = true, BulkCreation = false,
                LinkProtection = false, CampaignTracking = false, GeoDeviceTracking = false
            },
            new Domain.Entities.SubscriptionPlan
            {
                Id = enSubscriptionPlan.Professional, Name = "Professional", Description = "For growing teams",
                Price = 50, MaxQrCodesPerMonth = 10, MaxLinksPerMonth = 500, ClickDataRetentionDays = 90,
                LinkAnalysis = true, CustomShortCode = true, BulkCreation = true,
                LinkProtection = true, CampaignTracking = true, GeoDeviceTracking = false
            },
            new Domain.Entities.SubscriptionPlan
            {
                Id = enSubscriptionPlan.Enterprise, Name = "Enterprise", Description = "Unlimited everything",
                Price = 200, MaxQrCodesPerMonth = -1, MaxLinksPerMonth = -1, ClickDataRetentionDays = -1,
                LinkAnalysis = true, CustomShortCode = true, BulkCreation = true,
                LinkProtection = true, CampaignTracking = true, GeoDeviceTracking = true
            }
        );
    }
}
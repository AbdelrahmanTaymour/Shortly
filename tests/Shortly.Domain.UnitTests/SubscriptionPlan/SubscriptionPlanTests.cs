using Shortly.Domain.SubscriptionPlan;
using PlanEntity = Shortly.Domain.SubscriptionPlan.SubscriptionPlan;

namespace Shortly.Domain.UnitTests.SubscriptionPlan;

public class SubscriptionPlanTests
{
    private static PlanEntity Plan(int maxLinks = 100, int maxQrCodes = 10, int retentionDays = 30) =>
        new(enSubscriptionPlan.Starter, "Starter", "For individuals", 9.99m, maxLinks, maxQrCodes, retentionDays, 10, 3,
            linkAnalysis: true, bulkCreation: false, linkProtection: true,
            customShortCode: false, campaignTracking: true, geoDeviceTracking: false);

    [Fact]
    public void Constructor_Should_ExposeEveryConfiguredValue()
    {
        // Act
        var plan = Plan();

        // Assert
        plan.Id.Should().Be(enSubscriptionPlan.Starter);
        plan.Name.Should().Be("Starter");
        plan.Description.Should().Be("For individuals");
        plan.Price.Should().Be(9.99m);
        plan.MaxLinksPerMonth.Should().Be(100);
        plan.MaxQrCodesPerMonth.Should().Be(10);
        plan.ClickDataRetentionDays.Should().Be(30);
        plan.LinkAnalysis.Should().BeTrue();
        plan.BulkCreation.Should().BeFalse();
        plan.LinkProtection.Should().BeTrue();
        plan.CustomShortCode.Should().BeFalse();
        plan.CampaignTracking.Should().BeTrue();
        plan.GeoDeviceTracking.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(100, false)]
    public void HasUnlimitedLinks_Should_BeTrueOnlyForANegativeLimit(int maxLinks, bool expected)
    {
        // Assert
        Plan(maxLinks: maxLinks).HasUnlimitedLinks.Should().Be(expected);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(25, false)]
    public void HasUnlimitedQrCodes_Should_BeTrueOnlyForANegativeLimit(int maxQrCodes, bool expected)
    {
        // Assert
        Plan(maxQrCodes: maxQrCodes).HasUnlimitedQrCodes.Should().Be(expected);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(365, false)]
    public void HasUnlimitedRetention_Should_BeTrueOnlyForANegativeLimit(int retentionDays, bool expected)
    {
        // Assert
        Plan(retentionDays: retentionDays).HasUnlimitedRetention.Should().Be(expected);
    }

    [Fact]
    public void TheThreeUnlimitedFlags_Should_BeIndependent()
    {
        // Act
        var plan = Plan(maxLinks: -1, maxQrCodes: 5, retentionDays: -1);

        // Assert
        plan.HasUnlimitedLinks.Should().BeTrue();
        plan.HasUnlimitedQrCodes.Should().BeFalse();
        plan.HasUnlimitedRetention.Should().BeTrue();
    }

    [Fact]
    public void Plans_Should_DefineTheFourBusinessPlans_InAscendingOrder()
    {
        // Assert
        Enum.GetValues<enSubscriptionPlan>().Should().Equal(
            enSubscriptionPlan.Free, enSubscriptionPlan.Starter, enSubscriptionPlan.Professional, enSubscriptionPlan.Enterprise);
    }

    // ---- invariants --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_Throw_When_TheNameIsBlank(string? name)
    {
        // Act
        var act = () => new PlanEntity(enSubscriptionPlan.Free, name!, "desc", 0m, 1, 1, 1, 1, 1, false, false, false, false, false, false);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_Should_Throw_When_TheDescriptionIsNull()
    {
        // Act
        var act = () => new PlanEntity(enSubscriptionPlan.Free, "Free", null!, 0m, 1, 1, 1, 1, 1, false, false, false, false, false, false);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("description");
    }

    [Fact]
    public void Constructor_Should_Throw_When_ThePriceIsNegative()
    {
        // Act
        var act = () => new PlanEntity(enSubscriptionPlan.Free, "Free", "desc", -0.01m, 1, 1, 1, 1, 1, false, false, false, false, false, false);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("price");
    }

    [Fact]
    public void Constructor_Should_Accept_AFreePlanWithZeroLimits()
    {
        // Act
        var plan = new PlanEntity(enSubscriptionPlan.Free, "Free", "", 0m, 0, 0, 0, 1, 1, false, false, false, false, false, false);

        // Assert
        plan.Price.Should().Be(0m);
        plan.HasUnlimitedLinks.Should().BeFalse();
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(int.MinValue)]
    public void Constructor_Should_Throw_When_TheLinkLimitIsBelowMinusOne(int value)
    {
        // Act
        var act = () => Plan(maxLinks: value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxLinksPerMonth");
    }

    [Fact]
    public void Constructor_Should_Throw_When_TheQrCodeOrRetentionLimitIsBelowMinusOne()
    {
        // Assert
        ((Action)(() => Plan(maxQrCodes: -2))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxQrCodesPerMonth");
        ((Action)(() => Plan(retentionDays: -2))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("clickDataRetentionDays");
    }

    [Fact]
    public void Constructor_Should_Throw_When_ThePlanIdIsNotDefined()
    {
        // Act
        var act = () => new PlanEntity((enSubscriptionPlan)99, "Plan", "desc", 1m, 1, 1, 1, 1, 1, false, false, false, false, false, false);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("id");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Constructor_Should_Throw_When_TheMemberCapIsNeitherUnlimitedNorPositive(int cap)
    {
        // Act
        var act = () => new PlanEntity(enSubscriptionPlan.Free, "Free", "desc", 0m, 1, 1, 1, cap, 1, false, false, false, false, false, false);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxMembersPerOrganization");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Constructor_Should_Throw_When_TheOrganizationCapIsNeitherUnlimitedNorPositive(int cap)
    {
        // Act
        var act = () => new PlanEntity(enSubscriptionPlan.Free, "Free", "desc", 0m, 1, 1, 1, 1, cap, false, false, false, false, false, false);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxOrganizations");
    }

    [Fact]
    public void Constructor_Should_TreatMinusOneAsUnlimited_ForMembersAndOrganizations()
    {
        // Act
        var plan = new PlanEntity(enSubscriptionPlan.Enterprise, "Ent", "desc", 1m, 1, 1, 1, -1, -1, false, false, false, false, false, false);
        var limited = Plan();

        // Assert
        plan.HasUnlimitedMembers.Should().BeTrue();
        plan.HasUnlimitedOrganizations.Should().BeTrue();
        limited.HasUnlimitedMembers.Should().BeFalse();
        limited.MaxMembersPerOrganization.Should().Be(10);
        limited.MaxOrganizations.Should().Be(3);
    }
}

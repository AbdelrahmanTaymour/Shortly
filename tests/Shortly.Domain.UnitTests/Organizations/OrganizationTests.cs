using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.UnitTests.TestData;

namespace Shortly.Domain.UnitTests.Organizations;

public class OrganizationTests
{
    private static readonly DateTime Now = TestClock.Now;
    private static readonly DateTime Later = TestClock.Now.AddHours(1);

    // ---- creation ----------------------------------------------------------------------------------------------------

    [Fact]
    public void Create_Should_SetTheInitialState()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var result = Organization.Create("Acme", "Description", "https://acme.dev", "https://acme.dev/logo.png",
            25, ownerId, enSubscriptionPlan.Free, Now);
        var organization = result.Value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.Id.Should().NotBeEmpty();
        organization.Name.Should().Be("Acme");
        organization.Description.Should().Be("Description");
        organization.Website.Should().Be("https://acme.dev");
        organization.LogoUrl.Should().Be("https://acme.dev/logo.png");
        organization.MemberLimit.Should().Be(25);
        organization.OwnerId.Should().Be(ownerId);
        organization.SubscriptionPlanId.Should().Be(enSubscriptionPlan.Free);
        organization.IsActive.Should().BeTrue();
        organization.IsDeleted.Should().BeFalse();
        organization.DeletedAt.Should().BeNull();
        organization.CreatedAt.Should().Be(Now);
        organization.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_Should_StartWithEmptyUsage_ForTheNewOrganization()
    {
        // Act
        var organization = OrganizationData.CreateOrganization();

        // Assert
        organization.Usage.OrganizationId.Should().Be(organization.Id);
        organization.Usage.MonthlyLinksCreated.Should().Be(0);
        organization.Usage.MonthlyResetDate.Should().Be(Now.AddMonths(1));
    }

    [Fact]
    public void Create_Should_GenerateADifferentIdForEachOrganization()
    {
        // Act
        var first = OrganizationData.CreateOrganization();
        var second = OrganizationData.CreateOrganization();

        // Assert
        first.Id.Should().NotBe(second.Id);
    }

    [Theory]
    [InlineData("  Acme  ", "Acme")]
    [InlineData("Acme    Corp", "Acme Corp")]
    [InlineData("  Acme \t  Corp \n Ltd ", "Acme Corp Ltd")]
    public void Create_Should_TrimAndCollapseWhitespaceInTheName(string name, string expected)
    {
        // Act
        var result = Organization.Create(name, null, null, null, 10, Guid.NewGuid(), enSubscriptionPlan.Free, Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t\n ")]
    public void Create_Should_Fail_When_TheNameIsBlank(string? name)
    {
        // Act
        var result = Organization.Create(name!, null, null, null, 10, Guid.NewGuid(), enSubscriptionPlan.Free, Now);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NameRequired);
    }

    [Theory]
    [InlineData(enSubscriptionPlan.Starter, true)]
    [InlineData(enSubscriptionPlan.Professional, true)]
    [InlineData(enSubscriptionPlan.Enterprise, true)]
    [InlineData(enSubscriptionPlan.Free, false)]
    public void Create_Should_DeriveIsSubscribedFromThePlan(enSubscriptionPlan plan, bool subscribed)
    {
        // Act
        var organization = OrganizationData.CreateOrganization(plan: plan);

        // Assert
        organization.SubscriptionPlanId.Should().Be(plan);
        organization.IsSubscribed.Should().Be(subscribed);
    }

    // ---- details -----------------------------------------------------------------------------------------------------

    [Fact]
    public void UpdateDetails_Should_ReplaceEveryProvidedField_AndTouchTheTimestamp()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();

        // Act
        var result = organization.UpdateDetails("New name", "New description", "https://new.dev",
            "https://new.dev/logo.png", Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.Name.Should().Be("New name");
        organization.Description.Should().Be("New description");
        organization.Website.Should().Be("https://new.dev");
        organization.LogoUrl.Should().Be("https://new.dev/logo.png");
        organization.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void UpdateDetails_Should_LeaveFieldsUnchanged_When_NullIsPassed()
    {
        // Arrange
        var organization = Organization.Create("Acme", "Description", "https://acme.dev", "logo", 10,
            Guid.NewGuid(), enSubscriptionPlan.Free, Now).Value;

        // Act
        var result = organization.UpdateDetails(null, null, null, null, Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.Name.Should().Be("Acme");
        organization.Description.Should().Be("Description");
        organization.Website.Should().Be("https://acme.dev");
        organization.LogoUrl.Should().Be("logo");
        organization.MemberLimit.Should().Be(10);
        organization.UpdatedAt.Should().Be(Later);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_Should_Fail_AndChangeNothing_When_NameIsBlank(string name)
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();

        // Act
        var result = organization.UpdateDetails(name, "Changed", null, null, Later);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NameRequired);
        organization.Name.Should().Be("Acme");
        organization.Description.Should().BeNull();
        organization.MemberLimit.Should().Be(10);
        organization.UpdatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData("  Acme   Corp  ", "Acme Corp")]
    [InlineData("\tNew\nname ", "New name")]
    public void UpdateDetails_Should_TrimAndCollapseWhitespaceInTheName(string name, string expected)
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();

        // Act
        var result = organization.UpdateDetails(name, null, null, null, Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.Name.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_Should_ClearAnOptionalField_When_ItIsBlank(string blank)
    {
        // Arrange
        var organization = Organization.Create("Acme", "Description", "https://acme.dev", "logo", 10,
            Guid.NewGuid(), enSubscriptionPlan.Free, Now).Value;

        // Act
        var result = organization.UpdateDetails(null, blank, blank, blank, Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.Description.Should().BeNull();
        organization.Website.Should().BeNull();
        organization.LogoUrl.Should().BeNull();
        organization.Name.Should().Be("Acme");
    }

    [Fact]
    public void UpdateDetails_Should_OnlyClearTheFieldsThatWereBlank()
    {
        // Arrange
        var organization = Organization.Create("Acme", "Description", "https://acme.dev", "logo", 10,
            Guid.NewGuid(), enSubscriptionPlan.Free, Now).Value;

        // Act
        organization.UpdateDetails(null, "", null, null, Later);

        // Assert
        organization.Description.Should().BeNull();
        organization.Website.Should().Be("https://acme.dev");
        organization.LogoUrl.Should().Be("logo");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void SetMemberLimit_Should_Fail_AndChangeNothing_When_LimitIsBelowOne(int memberLimit)
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();

        // Act
        var result = organization.SetMemberLimit(memberLimit, Later);

        // Assert
        result.Error.Should().Be(OrganizationErrors.InvalidMemberLimit);
        organization.MemberLimit.Should().Be(10);
        organization.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void SetMemberLimit_Should_ReplaceTheLimit_AndTouchTheTimestamp()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();

        // Act
        var result = organization.SetMemberLimit(1, Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.MemberLimit.Should().Be(1);
        organization.UpdatedAt.Should().Be(Later);
    }

    // ---- activation and deletion ---------------------------------------------------------------------------------------

    [Fact]
    public void Deactivate_Should_MakeTheOrganizationInactive()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();

        // Act
        var result = organization.Deactivate(Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.IsActive.Should().BeFalse();
        organization.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void Deactivate_Should_Fail_When_AlreadyInactive()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();
        organization.Deactivate(Now);

        // Act
        var result = organization.Deactivate(Later);

        // Assert
        result.Error.Should().Be(OrganizationErrors.AlreadyInactive);
        organization.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void Activate_Should_MakeTheOrganizationActive()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();
        organization.Deactivate(Now);

        // Act
        var result = organization.Activate(Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.IsActive.Should().BeTrue();
        organization.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void Activate_Should_Fail_When_AlreadyActive()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();

        // Act
        var result = organization.Activate(Later);

        // Assert
        result.Error.Should().Be(OrganizationErrors.AlreadyActive);
    }

    [Fact]
    public void SoftDelete_Should_FlagTheOrganizationDeletedAndInactive()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();

        // Act
        var result = organization.SoftDelete(Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.IsDeleted.Should().BeTrue();
        organization.DeletedAt.Should().Be(Later);
        organization.IsActive.Should().BeFalse();
        organization.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void SoftDelete_Should_Fail_When_AlreadyDeleted()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();
        organization.SoftDelete(Now);

        // Act
        var result = organization.SoftDelete(Later);

        // Assert
        result.Error.Should().Be(OrganizationErrors.AlreadyDeleted);
        organization.DeletedAt.Should().Be(Now);
    }

    [Fact]
    public void Activate_Should_Fail_When_TheOrganizationIsDeleted()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();
        organization.SoftDelete(Now);

        // Act
        var result = organization.Activate(Later);

        // Assert
        result.Error.Should().Be(OrganizationErrors.Deleted);
        organization.IsActive.Should().BeFalse();
        organization.UpdatedAt.Should().Be(Now);
    }

    // ---- ownership ---------------------------------------------------------------------------------------------------

    [Fact]
    public void TransferOwnership_Should_SetTheNewOwner_AndTouchTheTimestamp()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();
        var newOwner = Guid.NewGuid();

        // Act
        var result = organization.TransferOwnership(newOwner, Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.OwnerId.Should().Be(newOwner);
        organization.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void TransferOwnership_Should_Fail_When_TheUserIsAlreadyTheOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var organization = OrganizationData.CreateOrganization(ownerId: ownerId);

        // Act
        var result = organization.TransferOwnership(ownerId, Later);

        // Assert
        result.Error.Should().Be(OrganizationErrors.AlreadyOwner);
        organization.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void TransferOwnership_Should_Fail_When_TheNewOwnerIsAnEmptyGuid()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();
        var ownerId = organization.OwnerId;

        // Act
        var result = organization.TransferOwnership(Guid.Empty, Later);

        // Assert
        result.Error.Should().Be(OrganizationErrors.InvalidOwner);
        organization.OwnerId.Should().Be(ownerId);
    }

    // ---- subscription ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(enSubscriptionPlan.Starter, true)]
    [InlineData(enSubscriptionPlan.Professional, true)]
    [InlineData(enSubscriptionPlan.Enterprise, true)]
    [InlineData(enSubscriptionPlan.Free, false)]
    public void ChangeSubscriptionPlan_Should_SetThePlan_AndFlagPaidPlansAsSubscribed(enSubscriptionPlan plan, bool subscribed)
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization(plan: enSubscriptionPlan.Professional);

        // Act
        var result = organization.ChangeSubscriptionPlan(plan, Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.SubscriptionPlanId.Should().Be(plan);
        organization.IsSubscribed.Should().Be(subscribed);
        organization.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void ChangeSubscriptionPlan_Should_ClearTheSubscribedFlag_WhenDowngradingToFree()
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization();
        organization.ChangeSubscriptionPlan(enSubscriptionPlan.Enterprise, Now);

        // Act
        organization.ChangeSubscriptionPlan(enSubscriptionPlan.Free, Later);

        // Assert
        organization.IsSubscribed.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void ChangeSubscriptionPlan_Should_Fail_AndChangeNothing_When_ThePlanIsNotDefined(int value)
    {
        // Arrange
        var organization = OrganizationData.CreateOrganization(plan: enSubscriptionPlan.Professional);

        // Act
        var result = organization.ChangeSubscriptionPlan((enSubscriptionPlan)value, Later);

        // Assert
        result.Error.Should().Be(OrganizationErrors.InvalidSubscriptionPlan);
        organization.SubscriptionPlanId.Should().Be(enSubscriptionPlan.Professional);
        organization.IsSubscribed.Should().BeTrue();
        organization.UpdatedAt.Should().Be(Now);
    }
}

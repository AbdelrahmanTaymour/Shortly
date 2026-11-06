using FluentValidation.TestHelper;
using Shortly.Core.Admin.ChangeOrganizationPlan;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Core.UnitTests.Admin;

public class ChangeOrganizationPlanCommandValidatorTests
{
    private readonly ChangeOrganizationPlanCommandValidator _sut = new();

    [Theory]
    [InlineData(enSubscriptionPlan.Free)]
    [InlineData(enSubscriptionPlan.Starter)]
    [InlineData(enSubscriptionPlan.Professional)]
    [InlineData(enSubscriptionPlan.Enterprise)]
    public void Validate_Should_Pass_ForEveryDefinedPlan(enSubscriptionPlan plan)
    {
        // Act
        var result = _sut.TestValidate(new ChangeOrganizationPlanCommand(Guid.NewGuid(), plan));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new ChangeOrganizationPlanCommand(Guid.Empty, enSubscriptionPlan.Free));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void Validate_Should_Fail_ForAnUndefinedPlan()
    {
        // Act
        var result = _sut.TestValidate(new ChangeOrganizationPlanCommand(Guid.NewGuid(), (enSubscriptionPlan)99));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubscriptionPlanId);
    }
}

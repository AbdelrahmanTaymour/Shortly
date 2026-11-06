using FluentValidation.TestHelper;
using Shortly.Core.Admin.ForceUpdateUser;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class ForceUpdateUserCommandValidatorTests
{
    private readonly ForceUpdateUserCommandValidator _sut = new();

    private static ForceUpdateUserCommand Empty() =>
        new(Guid.NewGuid(), null, null, null, null, null, null, null, null, null, null, null, null);

    [Fact]
    public void Validate_Should_Pass_WhenOnlyTheIdIsGiven()
    {
        // Act
        var result = _sut.TestValidate(Empty());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankUserId()
    {
        // Act
        var result = _sut.TestValidate(Empty() with { UserId = Guid.Empty });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_Should_Fail_ForAMalformedEmail_ButIgnoreAnEmptyOne()
    {
        // Act
        var malformed = _sut.TestValidate(Empty() with { Email = "nope" });
        var empty = _sut.TestValidate(Empty() with { Email = "" });

        // Assert
        malformed.ShouldHaveValidationErrorFor(x => x.Email);
        empty.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Should_Fail_ForAnUndefinedRoleOrPlan()
    {
        // Act
        var role = _sut.TestValidate(Empty() with { Role = (UserRole)99 });
        var plan = _sut.TestValidate(Empty() with { SubscriptionPlanId = (enSubscriptionPlan)99 });
        var valid = _sut.TestValidate(Empty() with { Role = UserRole.SystemAdmin, SubscriptionPlanId = enSubscriptionPlan.Starter });

        // Assert
        role.ShouldHaveValidationErrorFor(x => x.Role);
        plan.ShouldHaveValidationErrorFor(x => x.SubscriptionPlanId);
        valid.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(nameof(ForceUpdateUserCommand.Username), 50)]
    [InlineData(nameof(ForceUpdateUserCommand.ProfileName), 100)]
    [InlineData(nameof(ForceUpdateUserCommand.ProfileBio), 500)]
    [InlineData(nameof(ForceUpdateUserCommand.ProfilePhoneNumber), 50)]
    [InlineData(nameof(ForceUpdateUserCommand.ProfileCompany), 100)]
    [InlineData(nameof(ForceUpdateUserCommand.ProfileLocation), 100)]
    [InlineData(nameof(ForceUpdateUserCommand.ProfileCountry), 100)]
    [InlineData(nameof(ForceUpdateUserCommand.ProfileTimeZone), 100)]
    public void Validate_Should_EnforceEachTextFieldLengthBoundary(string property, int max)
    {
        // Arrange
        ForceUpdateUserCommand With(int length) => property switch
        {
            nameof(ForceUpdateUserCommand.Username) => Empty() with { Username = new string('a', length) },
            nameof(ForceUpdateUserCommand.ProfileName) => Empty() with { ProfileName = new string('a', length) },
            nameof(ForceUpdateUserCommand.ProfileBio) => Empty() with { ProfileBio = new string('a', length) },
            nameof(ForceUpdateUserCommand.ProfilePhoneNumber) => Empty() with { ProfilePhoneNumber = new string('a', length) },
            nameof(ForceUpdateUserCommand.ProfileCompany) => Empty() with { ProfileCompany = new string('a', length) },
            nameof(ForceUpdateUserCommand.ProfileLocation) => Empty() with { ProfileLocation = new string('a', length) },
            nameof(ForceUpdateUserCommand.ProfileCountry) => Empty() with { ProfileCountry = new string('a', length) },
            _ => Empty() with { ProfileTimeZone = new string('a', length) }
        };

        // Act
        var atLimit = _sut.TestValidate(With(max));
        var overLimit = _sut.TestValidate(With(max + 1));

        // Assert
        atLimit.ShouldNotHaveAnyValidationErrors();
        overLimit.ShouldHaveValidationErrorFor(property);
    }
}

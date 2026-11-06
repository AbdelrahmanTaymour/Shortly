using FluentValidation.TestHelper;
using Shortly.Core.Admin.LockUser;

namespace Shortly.Core.UnitTests.Admin;

public class LockUserCommandValidatorTests
{
    private readonly LockUserCommandValidator _sut = new();

    private static LockUserCommand Valid() => new(Guid.NewGuid(), "Suspicious activity", TimeSpan.FromHours(1));

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankUserId()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { UserId = Guid.Empty });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_ForABlankReason(string reason)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Reason = reason });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Validate_Should_EnforceTheReasonLengthBoundary()
    {
        // Act
        var atLimit = _sut.TestValidate(Valid() with { Reason = new string('a', 250) });
        var overLimit = _sut.TestValidate(Valid() with { Reason = new string('a', 251) });

        // Assert
        atLimit.ShouldNotHaveValidationErrorFor(x => x.Reason);
        overLimit.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Fail_ForANonPositiveDuration(int minutes)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Duration = TimeSpan.FromMinutes(minutes) });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Duration);
    }
}

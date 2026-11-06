using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.SetShortUrlPassword;

namespace Shortly.Core.UnitTests.ShortUrls;

public class SetShortUrlPasswordCommandValidatorTests
{
    private readonly SetShortUrlPasswordCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAPositiveIdAndAStrongEnoughPassword()
    {
        // Act
        var result = _sut.TestValidate(new SetShortUrlPasswordCommand(1, "Str0ng-password!"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Fail_ForAnIdThatIsNotPositive(long id)
    {
        // Act
        var result = _sut.TestValidate(new SetShortUrlPasswordCommand(id, "Str0ng-password!"));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public void Validate_Should_Fail_ForABlankOrTooShortPassword(string password)
    {
        // Act
        var result = _sut.TestValidate(new SetShortUrlPasswordCommand(1, password));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Validate_Should_EnforceThePasswordLengthBoundaries(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(new SetShortUrlPasswordCommand(1, new string('p', length)));

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Password);
        else result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

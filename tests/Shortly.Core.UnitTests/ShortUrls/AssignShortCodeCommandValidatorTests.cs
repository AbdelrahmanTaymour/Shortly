using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.AssignShortCode;

namespace Shortly.Core.UnitTests.ShortUrls;

public class AssignShortCodeCommandValidatorTests
{
    private readonly AssignShortCodeCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAPositiveIdAndAWellFormedCode()
    {
        // Act
        var result = _sut.TestValidate(new AssignShortCodeCommand(1, "my-code_1"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Fail_ForAnIdThatIsNotPositive(long id)
    {
        // Act
        var result = _sut.TestValidate(new AssignShortCodeCommand(id, "my-code"));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("has space")]
    [InlineData("dot.code")]
    [InlineData("code\n")]
    public void Validate_Should_Fail_ForABlankShortOrMalformedCode(string code)
    {
        // Act
        var result = _sut.TestValidate(new AssignShortCodeCommand(1, code));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewShortCode);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void Validate_Should_EnforceTheSameLengthLimitsAsTheDomain(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(new AssignShortCodeCommand(1, new string('a', length)));

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.NewShortCode);
        else result.ShouldHaveValidationErrorFor(x => x.NewShortCode);
    }
}

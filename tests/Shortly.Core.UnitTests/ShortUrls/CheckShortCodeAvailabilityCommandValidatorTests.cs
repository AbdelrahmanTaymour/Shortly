using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.CheckShortCodeAvailability;

namespace Shortly.Core.UnitTests.ShortUrls;

public class CheckShortCodeAvailabilityCommandValidatorTests
{
    private readonly CheckShortCodeAvailabilityCommandValidator _sut = new();

    [Theory]
    [InlineData("abcd")]
    [InlineData("my-code_1")]
    [InlineData("Ab12")]
    public void Validate_Should_Pass_ForAWellFormedCode(string code)
    {
        // Act
        var result = _sut.TestValidate(new CheckShortCodeAvailabilityCommand(code));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("has space")]
    [InlineData("slash/code")]
    [InlineData("dot.code")]
    [InlineData("code\n")]
    public void Validate_Should_Fail_ForABlankShortOrMalformedCode(string code)
    {
        // Act
        var result = _sut.TestValidate(new CheckShortCodeAvailabilityCommand(code));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortCode);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void Validate_Should_EnforceTheSameLengthLimitsAsTheDomain(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(new CheckShortCodeAvailabilityCommand(new string('a', length)));

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.ShortCode);
        else result.ShouldHaveValidationErrorFor(x => x.ShortCode);
    }
}

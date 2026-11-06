using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.DeactivateShortUrl;

namespace Shortly.Core.UnitTests.ShortUrls;

public class DeactivateShortUrlCommandValidatorTests
{
    private readonly DeactivateShortUrlCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAPositiveId()
    {
        // Act
        var result = _sut.TestValidate(new DeactivateShortUrlCommand(1));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Fail_ForAnIdThatIsNotPositive(long id)
    {
        // Act
        var result = _sut.TestValidate(new DeactivateShortUrlCommand(id));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlId);
    }
}

using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.DeleteShortUrl;

namespace Shortly.Core.UnitTests.ShortUrls;

public class DeleteShortUrlCommandValidatorTests
{
    private readonly DeleteShortUrlCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAPositiveId()
    {
        // Act
        var result = _sut.TestValidate(new DeleteShortUrlCommand(1));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Fail_ForAnIdThatIsNotPositive(long id)
    {
        // Act
        var result = _sut.TestValidate(new DeleteShortUrlCommand(id));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlId);
    }
}

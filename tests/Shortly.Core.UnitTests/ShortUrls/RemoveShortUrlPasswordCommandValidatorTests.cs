using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.RemoveShortUrlPassword;

namespace Shortly.Core.UnitTests.ShortUrls;

public class RemoveShortUrlPasswordCommandValidatorTests
{
    private readonly RemoveShortUrlPasswordCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAPositiveId()
    {
        // Act
        var result = _sut.TestValidate(new RemoveShortUrlPasswordCommand(1));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Fail_ForAnIdThatIsNotPositive(long id)
    {
        // Act
        var result = _sut.TestValidate(new RemoveShortUrlPasswordCommand(id));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlId);
    }
}

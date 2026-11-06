using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.BulkUpdateExpiration;
using Shortly.Core.UnitTests.TestData;

namespace Shortly.Core.UnitTests.ShortUrls;

public class BulkUpdateExpirationCommandValidatorTests
{
    private readonly BulkUpdateExpirationCommandValidator _sut = new(TestClock.Provider());

    [Fact]
    public void Validate_Should_Pass_ForDistinctIdsAndAFutureDate()
    {
        // Act
        var result = _sut.TestValidate(new BulkUpdateExpirationCommand([1, 2], TestClock.UtcNow.AddDays(1)));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_WhenTheExpirationIsCleared()
    {
        // Act
        var result = _sut.TestValidate(new BulkUpdateExpirationCommand([1], null));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    public void Validate_Should_Fail_WhenTheExpirationIsNotInTheFuture(int secondsFromNow)
    {
        // Act
        var result = _sut.TestValidate(new BulkUpdateExpirationCommand([1], TestClock.UtcNow.AddSeconds(secondsFromNow)));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void Validate_Should_Fail_ForNoIds()
    {
        // Act
        var result = _sut.TestValidate(new BulkUpdateExpirationCommand([], null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlIds);
    }

    [Fact]
    public void Validate_Should_Fail_ForMoreThan100Ids()
    {
        // Act
        var result = _sut.TestValidate(new BulkUpdateExpirationCommand(Enumerable.Range(1, 101).Select(i => (long)i).ToList(), null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlIds);
    }

    [Fact]
    public void Validate_Should_Fail_ForDuplicateIds()
    {
        // Act
        var result = _sut.TestValidate(new BulkUpdateExpirationCommand([1, 1], null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlIds);
    }

    [Fact]
    public void Validate_Should_Fail_WhenAnIdIsNotPositive()
    {
        // Act
        var result = _sut.TestValidate(new BulkUpdateExpirationCommand([1, 0], null));

        // Assert
        result.ShouldHaveAnyValidationError();
    }
}

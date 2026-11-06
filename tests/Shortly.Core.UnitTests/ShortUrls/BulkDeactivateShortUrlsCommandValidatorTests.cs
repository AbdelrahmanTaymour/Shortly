using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.BulkDeactivateShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class BulkDeactivateShortUrlsCommandValidatorTests
{
    private readonly BulkDeactivateShortUrlsCommandValidator _sut = new();

    private static BulkDeactivateShortUrlsCommand Command(IReadOnlyCollection<long> ids) => new(ids);

    [Fact]
    public void Validate_Should_Pass_ForAFewDistinctPositiveIds()
    {
        // Act
        var result = _sut.TestValidate(Command([1, 2, 3]));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForNoIds()
    {
        // Act
        var result = _sut.TestValidate(Command([]));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlIds);
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Validate_Should_LimitTheBatchTo100Ids(int count, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Command(Enumerable.Range(1, count).Select(i => (long)i).ToList()));

        // Assert
        if (valid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(x => x.ShortUrlIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_Should_Fail_WhenAnIdIsNotPositive(long badId)
    {
        // Act
        var result = _sut.TestValidate(Command([1, badId]));

        // Assert
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Validate_Should_Fail_ForDuplicateIds()
    {
        // Act
        var result = _sut.TestValidate(Command([1, 2, 2]));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlIds).WithErrorMessage("Duplicate short URL IDs are not allowed.");
    }
}

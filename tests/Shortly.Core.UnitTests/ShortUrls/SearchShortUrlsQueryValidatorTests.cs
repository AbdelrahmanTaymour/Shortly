using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.SearchShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class SearchShortUrlsQueryValidatorTests
{
    private readonly SearchShortUrlsQueryValidator _sut = new();

    private static SearchShortUrlsQuery Valid() =>
        new(null, ShortUrlStatusFilter.All, ShortUrlVisibilityFilter.All, null, null, ShortUrlSort.Newest, 1, 20);

    [Fact]
    public void Validate_Should_Pass_ForTheDefaultQuery()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Fail_ForAPageBelowOne(int page)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Page = page });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(101, false)]
    public void Validate_Should_EnforceThePageSizeBoundaries(int pageSize, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { PageSize = pageSize });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
        else result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Validate_Should_EnforceTheSearchTextLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Search = new string('s', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Search);
        else result.ShouldHaveValidationErrorFor(x => x.Search);
    }

    [Fact]
    public void Validate_Should_Fail_ForUndefinedFilterAndSortValues()
    {
        // Act
        var result = _sut.TestValidate(Valid() with
        {
            Status = (ShortUrlStatusFilter)99, Visibility = (ShortUrlVisibilityFilter)99, SortBy = (ShortUrlSort)99
        });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
        result.ShouldHaveValidationErrorFor(x => x.Visibility);
        result.ShouldHaveValidationErrorFor(x => x.SortBy);
    }

    [Fact]
    public void Validate_Should_Fail_WhenTheEndDateIsBeforeTheStartDate()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { DateFrom = new DateTime(2026, 2, 1), DateTo = new DateTime(2026, 1, 1) });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateTo);
    }

    [Fact]
    public void Validate_Should_Accept_TheSameStartAndEndDate()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { DateFrom = new DateTime(2026, 1, 1), DateTo = new DateTime(2026, 1, 1) });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DateTo);
    }

    [Fact]
    public void Validate_Should_Accept_AnOpenEndedDateRange()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { DateFrom = new DateTime(2026, 1, 1), DateTo = null });

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

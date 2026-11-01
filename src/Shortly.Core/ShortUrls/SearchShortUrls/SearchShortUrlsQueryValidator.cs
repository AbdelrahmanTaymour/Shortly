using FluentValidation;

namespace Shortly.Core.ShortUrls.SearchShortUrls;

internal sealed class SearchShortUrlsQueryValidator
    : AbstractValidator<SearchShortUrlsQuery>
{
    public SearchShortUrlsQueryValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(200);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Visibility)
            .IsInEnum();

        RuleFor(x => x.SortBy)
            .IsInEnum();

        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom!.Value)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("DateTo must be greater than or equal to DateFrom.");
    }
}
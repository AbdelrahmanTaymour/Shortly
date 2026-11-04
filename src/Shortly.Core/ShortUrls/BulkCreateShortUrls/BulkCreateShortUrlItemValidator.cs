using FluentValidation;

namespace Shortly.Core.ShortUrls.BulkCreateShortUrls;

internal sealed class BulkCreateShortUrlItemValidator
    : AbstractValidator<BulkCreateShortUrlItem>
{
    public BulkCreateShortUrlItemValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .MustBeHttpUrl();

        RuleFor(x => x.Title)
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}

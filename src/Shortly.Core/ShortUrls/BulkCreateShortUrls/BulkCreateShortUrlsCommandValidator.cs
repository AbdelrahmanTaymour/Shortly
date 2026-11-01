using FluentValidation;

namespace Shortly.Core.ShortUrls.BulkCreateShortUrls;

internal sealed class BulkCreateShortUrlsCommandValidator
    : AbstractValidator<BulkCreateShortUrlsCommand>
{
    public BulkCreateShortUrlsCommandValidator()
    {
        RuleFor(x => x.Urls)
            .NotEmpty()
            .Must(urls => urls.Count <= 100)
            .WithMessage("You can create a maximum of 100 short URLs at once.");

        RuleForEach(x => x.Urls)
            .SetValidator(new BulkCreateShortUrlItemValidator());
    }
}

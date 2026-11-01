using FluentValidation;

namespace Shortly.Core.ShortUrls.BulkActivateShortUrls;

internal sealed class BulkActivateShortUrlsCommandValidator
    : AbstractValidator<BulkActivateShortUrlsCommand>
{
    public BulkActivateShortUrlsCommandValidator()
    {
        RuleFor(x => x.ShortUrlIds)
            .NotEmpty()
            .Must(ids => ids.Count <= 100)
            .WithMessage("You can process a maximum of 100 short URLs at once.");

        RuleForEach(x => x.ShortUrlIds)
            .GreaterThan(0);

        RuleFor(x => x.ShortUrlIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate short URL IDs are not allowed.");
    }
}

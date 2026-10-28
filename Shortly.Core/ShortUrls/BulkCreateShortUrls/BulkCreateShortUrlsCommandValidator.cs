using FluentValidation;

namespace Shortly.Core.ShortUrls.BulkCreateShortUrls;

public sealed class BulkCreateShortUrlsCommandValidator : AbstractValidator<BulkCreateShortUrlsCommand>
{
    public BulkCreateShortUrlsCommandValidator()
    {
        RuleFor(c => c.Urls).NotEmpty().Must(u => u.Count <= 100)
            .WithMessage("A bulk create request can contain at most 100 URLs.");
    }
}

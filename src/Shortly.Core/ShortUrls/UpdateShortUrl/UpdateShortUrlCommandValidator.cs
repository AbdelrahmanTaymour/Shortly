using FluentValidation;
using Shortly.Core.Abstractions.Clock;

namespace Shortly.Core.ShortUrls.UpdateShortUrl;

internal sealed class UpdateShortUrlCommandValidator
    : AbstractValidator<UpdateShortUrlCommand>
{
    public UpdateShortUrlCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.ShortUrlId)
            .GreaterThan(0);

        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .MustBeHttpUrl();

        RuleFor(x => x.ShortCode)
            .NotEmpty()
            .MustBeShortCodeFormat();

        RuleFor(x => x.Title)
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        // -1 means unlimited everywhere else (domain, redirect, creation), so it must stay a valid value here.
        RuleFor(x => x.ClickLimit)
            .Must(limit => limit == -1 || limit > 0)
            .WithMessage("Click limit must be greater than zero, or -1 for unlimited.");

        RuleFor(x => x.ExpiresAt)
            .MustBeInTheFuture(clock)
            .When(x => x.ExpiresAt.HasValue);
    }
}

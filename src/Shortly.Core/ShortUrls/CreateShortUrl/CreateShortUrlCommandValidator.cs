using FluentValidation;
using Shortly.Core.Abstractions.Clock;

namespace Shortly.Core.ShortUrls.CreateShortUrl;

internal sealed class CreateShortUrlCommandValidator
    : AbstractValidator<CreateShortUrlCommand>
{
    public CreateShortUrlCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .MustBeHttpUrl();

        RuleFor(x => x.CustomShortCode)
            .MustBeShortCodeFormat()
            .When(x => !string.IsNullOrWhiteSpace(x.CustomShortCode));

        RuleFor(x => x.ClickLimit)
            .GreaterThan(0)
            .When(x => x.ClickLimit.HasValue);

        RuleFor(x => x.ExpiresAt)
            .MustBeInTheFuture(clock)
            .When(x => x.ExpiresAt.HasValue);

        RuleFor(x => x.Title)
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}

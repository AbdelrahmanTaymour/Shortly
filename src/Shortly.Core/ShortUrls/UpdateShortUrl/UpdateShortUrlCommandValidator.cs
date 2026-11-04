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

        RuleFor(x => x.ClickLimit)
            .GreaterThan(0);

        RuleFor(x => x.ExpiresAt)
            .MustBeInTheFuture(clock)
            .When(x => x.ExpiresAt.HasValue);
    }
}

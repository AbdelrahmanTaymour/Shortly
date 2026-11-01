using FluentValidation;

namespace Shortly.Core.ShortUrls.ActivateShortUrl;

internal sealed class ActivateShortUrlCommandValidator
    : AbstractValidator<ActivateShortUrlCommand>
{
    public ActivateShortUrlCommandValidator()
    {
        RuleFor(x => x.ShortUrlId).GreaterThan(0);
    }
}

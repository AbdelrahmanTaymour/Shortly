using FluentValidation;

namespace Shortly.Core.ShortUrls.DeactivateShortUrl;

internal sealed class DeactivateShortUrlCommandValidator
    : AbstractValidator<DeactivateShortUrlCommand>
{
    public DeactivateShortUrlCommandValidator()
    {
        RuleFor(x => x.ShortUrlId).GreaterThan(0);
    }
}
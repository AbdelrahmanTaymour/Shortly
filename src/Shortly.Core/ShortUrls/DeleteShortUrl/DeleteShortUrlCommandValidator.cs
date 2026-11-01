using FluentValidation;

namespace Shortly.Core.ShortUrls.DeleteShortUrl;

internal sealed class DeleteShortUrlCommandValidator
    : AbstractValidator<DeleteShortUrlCommand>
{
    public DeleteShortUrlCommandValidator()
    {
        RuleFor(x => x.ShortUrlId).GreaterThan(0);
    }
}

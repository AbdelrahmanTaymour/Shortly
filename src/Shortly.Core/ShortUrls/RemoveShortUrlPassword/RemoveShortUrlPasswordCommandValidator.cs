using FluentValidation;

namespace Shortly.Core.ShortUrls.RemoveShortUrlPassword;

internal sealed class RemoveShortUrlPasswordCommandValidator
    : AbstractValidator<RemoveShortUrlPasswordCommand>
{
    public RemoveShortUrlPasswordCommandValidator()
    {
        RuleFor(x => x.ShortUrlId).GreaterThan(0);
    }
}

using FluentValidation;

namespace Shortly.Core.ShortUrls.SetShortUrlPassword;

internal sealed class SetShortUrlPasswordCommandValidator
    : AbstractValidator<SetShortUrlPasswordCommand>
{
    public SetShortUrlPasswordCommandValidator()
    {
        RuleFor(x => x.ShortUrlId).GreaterThan(0);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);
    }
}
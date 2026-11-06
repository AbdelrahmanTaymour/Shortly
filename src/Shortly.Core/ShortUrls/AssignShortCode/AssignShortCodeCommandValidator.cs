using FluentValidation;

namespace Shortly.Core.ShortUrls.AssignShortCode;

internal sealed class AssignShortCodeCommandValidator
    : AbstractValidator<AssignShortCodeCommand>
{
    public AssignShortCodeCommandValidator()
    {
        RuleFor(x => x.ShortUrlId).GreaterThan(0);

        RuleFor(x => x.NewShortCode)
            .NotEmpty()
            .MustBeShortCodeFormat();
    }
}

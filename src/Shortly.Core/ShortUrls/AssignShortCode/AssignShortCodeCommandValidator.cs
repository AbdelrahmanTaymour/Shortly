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
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Short code can only contain letters, numbers, hyphens, and underscores.");
    }
}

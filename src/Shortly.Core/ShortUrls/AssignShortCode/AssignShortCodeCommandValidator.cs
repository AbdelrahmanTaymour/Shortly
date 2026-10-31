using FluentValidation;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.AssignShortCode;

public sealed class AssignShortCodeCommandValidator : AbstractValidator<AssignShortCodeCommand>
{
    public AssignShortCodeCommandValidator()
    {
        RuleFor(c => c.NewShortCode)
            .NotEmpty()
            .Length(ShortCode.MinLength, ShortCode.MaxLength)
            .Matches("^[a-zA-Z0-9_-]+$");
    }
}

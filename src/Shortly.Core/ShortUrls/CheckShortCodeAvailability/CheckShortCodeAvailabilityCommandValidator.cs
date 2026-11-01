using FluentValidation;

namespace Shortly.Core.ShortUrls.CheckShortCodeAvailability;

internal sealed class CheckShortCodeAvailabilityCommandValidator : AbstractValidator<CheckShortCodeAvailabilityCommand>
{
    public CheckShortCodeAvailabilityCommandValidator()
    {
        RuleFor(x => x.ShortCode)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Short code can only contain letters, numbers, hyphens, and underscores.");
    }
}
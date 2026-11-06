using FluentValidation;

namespace Shortly.Core.ShortUrls.CheckShortCodeAvailability;

internal sealed class CheckShortCodeAvailabilityCommandValidator : AbstractValidator<CheckShortCodeAvailabilityCommand>
{
    public CheckShortCodeAvailabilityCommandValidator()
    {
        RuleFor(x => x.ShortCode)
            .NotEmpty()
            .MustBeShortCodeFormat();
    }
}
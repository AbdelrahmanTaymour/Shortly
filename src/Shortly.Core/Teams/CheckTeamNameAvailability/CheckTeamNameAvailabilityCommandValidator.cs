using FluentValidation;

namespace Shortly.Core.Teams.CheckTeamNameAvailability;

internal sealed class CheckTeamNameAvailabilityCommandValidator : AbstractValidator<CheckTeamNameAvailabilityCommand>
{
    public CheckTeamNameAvailabilityCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ExcludeTeamId)
            .NotEqual(Guid.Empty)
            .When(x => x.ExcludeTeamId.HasValue);
    }
}

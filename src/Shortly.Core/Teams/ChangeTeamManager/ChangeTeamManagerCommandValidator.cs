using FluentValidation;

namespace Shortly.Core.Teams.ChangeTeamManager;

internal sealed class ChangeTeamManagerCommandValidator
    : AbstractValidator<ChangeTeamManagerCommand>
{
    public ChangeTeamManagerCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();

        RuleFor(x => x.TeamId)
            .NotEmpty();

        RuleFor(x => x.NewManagerId)
            .NotEmpty();
    }
}

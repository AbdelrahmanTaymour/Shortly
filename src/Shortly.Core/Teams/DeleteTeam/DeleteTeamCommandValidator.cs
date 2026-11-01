using FluentValidation;

namespace Shortly.Core.Teams.DeleteTeam;

internal sealed class DeleteTeamCommandValidator
    : AbstractValidator<DeleteTeamCommand>
{
    public DeleteTeamCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();

        RuleFor(x => x.TeamId)
            .NotEmpty();
    }
}

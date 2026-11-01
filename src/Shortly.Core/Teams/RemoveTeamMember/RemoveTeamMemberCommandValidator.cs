using FluentValidation;

namespace Shortly.Core.Teams.RemoveTeamMember;

internal sealed class RemoveTeamMemberCommandValidator
    : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();

        RuleFor(x => x.TeamId)
            .NotEmpty();

        RuleFor(x => x.MemberId)
            .NotEmpty();
    }
}

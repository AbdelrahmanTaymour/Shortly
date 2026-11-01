using FluentValidation;

namespace Shortly.Core.Teams.AddTeamMember;

internal sealed class AddTeamMemberCommandValidator
    : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();

        RuleFor(x => x.TeamId)
            .NotEmpty();

        RuleFor(x => x.MemberId)
            .NotEmpty();
    }
}

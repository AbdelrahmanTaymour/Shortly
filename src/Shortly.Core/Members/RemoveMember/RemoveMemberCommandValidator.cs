using FluentValidation;

namespace Shortly.Core.Members.RemoveMember;

internal sealed class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}

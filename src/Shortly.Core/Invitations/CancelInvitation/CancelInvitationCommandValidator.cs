using FluentValidation;

namespace Shortly.Core.Invitations.CancelInvitation;

public sealed class CancelInvitationCommandValidator : AbstractValidator<CancelInvitationCommand>
{
    public CancelInvitationCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.InvitationId).NotEmpty();
    }
}

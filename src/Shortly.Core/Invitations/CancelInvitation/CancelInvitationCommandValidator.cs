using FluentValidation;

namespace Shortly.Core.Invitations.CancelInvitation;

internal sealed class CancelInvitationCommandValidator : AbstractValidator<CancelInvitationCommand>
{
    public CancelInvitationCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.InvitationId).NotEmpty();
    }
}

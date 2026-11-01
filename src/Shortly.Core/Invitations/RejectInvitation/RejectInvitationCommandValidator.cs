using FluentValidation;

namespace Shortly.Core.Invitations.RejectInvitation;

public sealed class RejectInvitationCommandValidator : AbstractValidator<RejectInvitationCommand>
{
    public RejectInvitationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(2048);
    }
}

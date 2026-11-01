using FluentValidation;

namespace Shortly.Core.Invitations.MarkInvitationSent;

public sealed class MarkInvitationSentCommandValidator : AbstractValidator<MarkInvitationSentCommand>
{
    public MarkInvitationSentCommandValidator()
    {
        RuleFor(x => x.InvitationId).NotEmpty();
    }
}

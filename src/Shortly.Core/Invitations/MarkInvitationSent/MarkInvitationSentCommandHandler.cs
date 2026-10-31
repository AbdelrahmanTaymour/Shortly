using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;

namespace Shortly.Core.Invitations.MarkInvitationSent;

internal sealed class MarkInvitationSentCommandHandler(IOrganizationInvitationRepository invitationRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<MarkInvitationSentCommand>
{
    public async Task<Result> Handle(MarkInvitationSentCommand request, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null) return Result.Failure(OrganizationInvitationErrors.NotFound);

        var result = invitation.MarkAsSent();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
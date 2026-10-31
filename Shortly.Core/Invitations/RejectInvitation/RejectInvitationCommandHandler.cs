using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;

namespace Shortly.Core.Invitations.RejectInvitation;

internal sealed class RejectInvitationCommandHandler(
    IOrganizationInvitationRepository invitationRepository, ITokenHasher tokenHasher, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RejectInvitationCommand>
{
    public async Task<Result> Handle(RejectInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByTokenHashAsync(tokenHasher.Hash(request.Token), cancellationToken);
        if (invitation is null) return Result.Failure(OrganizationInvitationErrors.NotFound);

        var result = invitation.Reject(dateTimeProvider.UtcNow);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
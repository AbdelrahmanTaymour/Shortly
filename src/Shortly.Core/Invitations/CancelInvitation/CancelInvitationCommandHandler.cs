using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;

namespace Shortly.Core.Invitations.CancelInvitation;

internal sealed class CancelInvitationCommandHandler(
    IUserContext userContext, IOrganizationInvitationRepository invitationRepository, IOrganizationMemberRepository memberRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelInvitationCommand>
{
    public async Task<Result> Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null || invitation.OrganizationId != request.OrganizationId)
            return Result.Failure(OrganizationInvitationErrors.NotFound);

        var isInviter = invitation.InvitedBy == userContext.UserId;
        if (!isInviter)
        {
            var caller = await memberRepository.GetByOrganizationAndUserAsync(invitation.OrganizationId, userContext.UserId, cancellationToken);
            if (caller is null || caller.Role < MemberRole.OrgAdmin)
                return Result.Failure(OrganizationInvitationErrors.NotInviter);
        }

        invitationRepository.Remove(invitation);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
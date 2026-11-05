using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;

namespace Shortly.Core.Invitations.CancelInvitation;

internal sealed class CancelInvitationCommandHandler(
    IUserContext userContext, IOrganizationInvitationRepository invitationRepository, IOrganizationAuthorizationService organizationAuthorization, IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CancelInvitationCommand>
{
    public async Task<Result> Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null || invitation.OrganizationId != request.OrganizationId)
            return Result.Failure(OrganizationInvitationErrors.NotFound);

        var membership = await organizationAuthorization.RequireMembershipAsync(invitation.OrganizationId, cancellationToken);
        if (membership.IsFailure) return Result.Failure(membership.Error);

        // The original inviter or an organization admin; either way they must still be an active member.
        var isInviter = invitation.InvitedBy == userContext.UserId;
        if (!isInviter && membership.Value.Role < MemberRole.OrgAdmin)
            return Result.Failure(OrganizationInvitationErrors.NotInviter);

        var cancelResult = invitation.Cancel(dateTimeProvider.UtcNow);
        if (cancelResult.IsFailure) return cancelResult;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
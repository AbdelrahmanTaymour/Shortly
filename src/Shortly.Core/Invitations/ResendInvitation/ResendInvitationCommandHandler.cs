using Microsoft.Extensions.Configuration;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Invitations.ResendInvitation;

internal sealed class ResendInvitationCommandHandler(
    IUserContext userContext,
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationMemberRepository memberRepository,
    IOrganizationRepository organizationRepository,
    IEmailNotifier notifier,
    IConfiguration configuration,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ResendInvitationCommand>
{
    public async Task<Result> Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
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

        var organization = await organizationRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure(OrganizationErrors.NotFound);

        var plainTextToken = tokenHasher.GenerateToken();
        var tokenHash = tokenHasher.Hash(plainTextToken);
        var expiryDays = configuration.GetValue("AppSettings:Tokens:InvitationExpiryDays", 7);

        var regenerateResult = invitation.Regenerate(tokenHash, dateTimeProvider.UtcNow.AddDays(expiryDays));
        if (regenerateResult.IsFailure) return regenerateResult;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var inviteeName = invitation.InvitedUserEmail.Split('@')[0];
        notifier.EnqueueSendUserInvitation(invitation.InvitedUserEmail, inviteeName, inviteeName, plainTextToken, organization.Name);
        
        return Result.Success();
    }
}
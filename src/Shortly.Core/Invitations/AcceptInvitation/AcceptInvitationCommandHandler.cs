using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Core.Invitations.AcceptInvitation;

internal sealed class AcceptInvitationCommandHandler(
    IUserContext userContext,
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationMemberRepository memberRepository,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AcceptInvitationCommand>
{
    public async Task<Result> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenHasher.Hash(request.Token);
        var invitation = await invitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (invitation is null) return Result.Failure(OrganizationInvitationErrors.NotFound);

        var callerUser = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
        if (callerUser is null || !string.Equals(callerUser.Email, invitation.InvitedUserEmail, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(OrganizationInvitationErrors.EmailMismatch);

        // An inactive or deleted organization cannot take new members. Checked before the invitation is consumed.
        var organization = await organizationRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure(OrganizationErrors.NotFound);
        if (!organization.IsActive) return Result.Failure(OrganizationErrors.Inactive);

        var existing = await memberRepository.GetByOrganizationAndUserAsync(
            invitation.OrganizationId, userContext.UserId, cancellationToken);
        if (existing is { IsActive: true })
            return Result.Failure(OrganizationInvitationErrors.AlreadyMember);

        // Accepting adds an active member (new or reactivated), so it is subject to the same limit as AddMember.
        var activeMembers = await memberRepository.GetMemberCountByOrganizationAsync(invitation.OrganizationId, cancellationToken);
        if (activeMembers >= organization.MemberLimit)
            return Result.Failure(OrganizationInvitationErrors.MemberLimitReached);

        var utcNow = dateTimeProvider.UtcNow;
        var acceptResult = invitation.Accept(utcNow);
        if (acceptResult.IsFailure) return acceptResult;

        if (existing is not null)
        {
            // A previously removed member keeps their (inactive) row; it is reactivated instead of inserting a duplicate.
            var reactivated = existing.Reactivate();
            if (reactivated.IsFailure) return reactivated;

            var roleChanged = existing.ChangeRole(invitation.InvitedUserRole);
            if (roleChanged.IsFailure) return roleChanged;
        }
        else
        {
            memberRepository.Add(OrganizationMember.Invite(
                invitation.OrganizationId, userContext.UserId, invitation.InvitedUserRole, invitation.InvitedBy, utcNow));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken); 
        return Result.Success();
    }
}
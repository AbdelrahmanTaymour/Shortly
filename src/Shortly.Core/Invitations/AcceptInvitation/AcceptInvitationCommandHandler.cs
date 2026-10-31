using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Core.Invitations.AcceptInvitation;

internal sealed class AcceptInvitationCommandHandler(
    IUserContext userContext,
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationMemberRepository memberRepository,
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

        var utcNow = dateTimeProvider.UtcNow;
        var acceptResult = invitation.Accept(utcNow);
        if (acceptResult.IsFailure) return acceptResult;

        var member = OrganizationMember.Invite(invitation.OrganizationId, userContext.UserId, invitation.InvitedUserRole, invitation.InvitedBy, utcNow);
        memberRepository.Add(member);

        await unitOfWork.SaveChangesAsync(cancellationToken); 
        return Result.Success();
    }
}
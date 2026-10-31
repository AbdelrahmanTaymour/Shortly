using Microsoft.Extensions.Configuration;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Core.Invitations.CreateInvitation;

internal sealed class CreateInvitationCommandHandler(
    IUserContext userContext,
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    IOrganizationInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IEmailNotifier notifier,
    IConfiguration configuration,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateInvitationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        if (request.Role == MemberRole.OrgOwner)
            return Result.Failure<Guid>(OrganizationInvitationErrors.CannotInviteAsOwner);

        var caller = await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, userContext.UserId, cancellationToken);
        if (caller is null || caller.Role < MemberRole.OrgAdmin)
            return Result.Failure<Guid>(OrganizationInvitationErrors.InsufficientRole);

        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure<Guid>(OrganizationErrors.NotFound);

        var currentMemberCount = await memberRepository.GetMemberCountByOrganizationAsync(request.OrganizationId, cancellationToken);
        if (currentMemberCount >= organization.MemberLimit)
            return Result.Failure<Guid>(OrganizationInvitationErrors.MemberLimitReached);

        var utcNow = dateTimeProvider.UtcNow;
        if (await invitationRepository.HasPendingInvitationAsync(request.Email, request.OrganizationId, utcNow, cancellationToken))
            return Result.Failure<Guid>(OrganizationInvitationErrors.AlreadyPending);

        var existingUser = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null &&
            await memberRepository.IsMemberOfOrganizationAsync(existingUser.Id, request.OrganizationId, cancellationToken))
            return Result.Failure<Guid>(OrganizationInvitationErrors.AlreadyMember);

        var expiryDays = configuration.GetValue("AppSettings:Tokens:InvitationExpiryDays", 7);
        var plainTextToken = tokenHasher.GenerateToken();
        var tokenHash = tokenHasher.Hash(plainTextToken);

        var invitation = OrganizationInvitation.Create(
            request.OrganizationId, request.Email, request.Role, request.CustomPermissions,
            userContext.UserId, tokenHash, utcNow.AddDays(expiryDays), utcNow);

        invitationRepository.Add(invitation);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var inviteeName = request.Email.Split('@')[0];
        notifier.EnqueueSendUserInvitation(request.Email, inviteeName, inviteeName, plainTextToken, organization.Name);
        return invitation.Id;
    }
}
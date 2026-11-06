using Shortly.Domain.Users;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Members.AddMember;

internal sealed class AddMemberCommandHandler(
    IUserContext userContext,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IOrganizationAuthorizationService organizationAuthorization,
    IOrganizationMemberRepository memberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddMemberCommand>
{
    public async Task<Result> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var caller = await organizationAuthorization.RequireRoleAsync(request.OrganizationId, MemberRole.OrgAdmin, cancellationToken);
        if (caller.IsFailure) return Result.Failure(caller.Error);

        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure(OrganizationErrors.NotFound);

        if (request.Role == MemberRole.OrgOwner)
            return Result.Failure(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);

        if (!await userRepository.ExistsAsync(request.UserId, cancellationToken))
            return Result.Failure(UserErrors.NotFound);

        var existing = await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, request.UserId,
            cancellationToken);
        if (existing is { IsActive: true })
            return Result.Failure(OrganizationMemberErrors.AlreadyMember);

        var currentCount =
            await memberRepository.GetMemberCountByOrganizationAsync(request.OrganizationId, cancellationToken);
        if (currentCount >= organization.MemberLimit)
            return Result.Failure(OrganizationMemberErrors.MemberLimitReached);

        if (existing is not null)
        {
            var reactivated = existing.Reactivate(request.Role, dateTimeProvider.UtcNow);
            if (reactivated.IsFailure) return reactivated;
        }
        else
        {
            var invited = OrganizationMember.Invite(request.OrganizationId, request.UserId, request.Role,
                userContext.UserId, dateTimeProvider.UtcNow);
            if (invited.IsFailure) return invited;

            memberRepository.Add(invited.Value);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
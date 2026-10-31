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
    IOrganizationMemberRepository memberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddMemberCommand>
{
    public async Task<Result> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure(OrganizationErrors.NotFound);

        var caller =
            await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, userContext.UserId,
                cancellationToken);
        if (caller is null || caller.Role < MemberRole.OrgAdmin)
            return Result.Failure(OrganizationMemberErrors.InsufficientRole);

        if (request.Role == MemberRole.OrgOwner)
            return Result.Failure(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);

        if (await memberRepository.IsMemberOfOrganizationAsync(request.UserId, request.OrganizationId,
                cancellationToken))
            return Result.Failure(OrganizationMemberErrors.AlreadyMember);

        var currentCount =
            await memberRepository.GetMemberCountByOrganizationAsync(request.OrganizationId, cancellationToken);
        if (currentCount >= organization.MemberLimit)
            return Result.Failure(OrganizationMemberErrors.MemberLimitReached);

        var member = OrganizationMember.Invite(request.OrganizationId, request.UserId, request.Role, userContext.UserId,
            dateTimeProvider.UtcNow);
        memberRepository.Add(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
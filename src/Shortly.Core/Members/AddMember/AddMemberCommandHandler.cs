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
    IOrganizationAuthorizationService organizationAuthorization,
    IOrganizationMemberRepository memberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddMemberCommand>
{
    public async Task<Result> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure(OrganizationErrors.NotFound);

        var caller = await organizationAuthorization.RequireRoleAsync(request.OrganizationId, MemberRole.OrgAdmin, cancellationToken);
        if (caller.IsFailure) return Result.Failure(caller.Error);

        if (request.Role == MemberRole.OrgOwner)
            return Result.Failure(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);

        // A previously removed member keeps their (inactive) row; it is reactivated instead of inserting a duplicate.
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
            var reactivated = existing.Reactivate();
            if (reactivated.IsFailure) return reactivated;

            var roleChanged = existing.ChangeRole(request.Role);
            if (roleChanged.IsFailure) return roleChanged;
        }
        else
        {
            memberRepository.Add(OrganizationMember.Invite(request.OrganizationId, request.UserId, request.Role,
                userContext.UserId, dateTimeProvider.UtcNow));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
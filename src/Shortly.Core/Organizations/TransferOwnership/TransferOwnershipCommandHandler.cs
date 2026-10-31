using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Organizations.TransferOwnership;

internal sealed class TransferOwnershipCommandHandler(
    IUserContext userContext,
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<TransferOwnershipCommand>
{
    public async Task<Result> Handle(TransferOwnershipCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure(OrganizationErrors.NotFound);

        if (organization.OwnerId != userContext.UserId)
            return Result.Failure(OrganizationErrors.NotOwner);

        var oldOwnerMembership =
            await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, organization.OwnerId,
                cancellationToken);
        
        var newOwnerMembership =
            await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, request.NewOwnerId,
                cancellationToken);

        if (newOwnerMembership is null || !newOwnerMembership.IsActive)
            return Result.Failure(OrganizationErrors.NewOwnerNotMember);

        if (oldOwnerMembership is null)
            return Result.Failure(OrganizationMemberErrors.NotFound);

        var transferResult = organization.TransferOwnership(request.NewOwnerId, dateTimeProvider.UtcNow);
        if (transferResult.IsFailure) return transferResult;

        var demoteResult = oldOwnerMembership.DemoteFromOwner(MemberRole.OrgAdmin);
        if (demoteResult.IsFailure) return demoteResult;

        newOwnerMembership.PromoteToOwner();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
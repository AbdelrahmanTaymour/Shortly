using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.RepositoryContract.Members;

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

        var isNewOwnerMember = await memberRepository.IsMemberOfOrganizationAsync(request.NewOwnerId, request.OrganizationId, cancellationToken);
        if (!isNewOwnerMember)
            return Result.Failure(OrganizationErrors.NewOwnerNotMember);

        var result = organization.TransferOwnership(request.NewOwnerId, dateTimeProvider.UtcNow);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
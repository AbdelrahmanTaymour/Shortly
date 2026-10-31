using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Organizations.DeactivateOrganization;

internal sealed class DeactivateOrganizationCommandHandler(
    IOrganizationRepository organizationRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DeactivateOrganizationCommand>
{
    public async Task<Result> Handle(DeactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure(OrganizationErrors.NotFound);

        if (!userContext.IsAdmin || !userContext.IsOwner(organization.OwnerId))
            return Result.Failure(OrganizationErrors.NotOwner);

        var result = organization.Deactivate(dateTimeProvider.UtcNow);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Organizations.UpdateOrganization;

internal sealed class UpdateOrganizationCommandHandler(
    IUserContext userContext,
    IOrganizationRepository organizationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateOrganizationCommand>
{
    public async Task<Result> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure(OrganizationErrors.NotFound);

        if (organization.OwnerId != userContext.UserId)
            return Result.Failure(OrganizationErrors.NotOwner);

        var result = organization.UpdateDetails(request.Name, request.Description, request.Website, request.LogoUrl, request.MemberLimit, dateTimeProvider.UtcNow);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
using Shortly.Core.Exceptions;
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
    private const string OrganizationNameIndex = "ux_organizations_owner_id_name";

    public async Task<Result> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure(OrganizationErrors.NotFound);

        if (organization.OwnerId != userContext.UserId)
            return Result.Failure(OrganizationErrors.NotOwner);

        var result = organization.UpdateDetails(request.Name, request.Description, request.Website, request.LogoUrl, dateTimeProvider.UtcNow);
        if (result.IsFailure) return result;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == OrganizationNameIndex)
        {
            return Result.Failure(OrganizationErrors.NameTaken);
        }

        return Result.Success();
    }
}
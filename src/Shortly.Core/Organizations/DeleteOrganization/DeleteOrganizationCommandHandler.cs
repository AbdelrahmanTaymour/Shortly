using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.Organizations.DeleteOrganization;

internal sealed class DeleteOrganizationCommandHandler(
    IUserContext userContext,
    IOrganizationRepository organizationRepository,
    IShortUrlRepository shortUrlRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteOrganizationCommand>
{
    public async Task<Result> Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure(OrganizationErrors.NotFound);

        if (organization.OwnerId != userContext.UserId)
            return Result.Failure(OrganizationErrors.NotOwner);

        var result = organization.SoftDelete(dateTimeProvider.UtcNow);
        if (result.IsFailure) return result;

        // The organization's links (and, by cascade, their click events) go with it, so nothing keeps redirecting.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await shortUrlRepository.DeleteAllForOrganizationAsync(organization.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return Result.Success();
    }
}
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Admin.ChangeOrganizationPlan;

internal sealed class ChangeOrganizationPlanCommandHandler(
    IOrganizationRepository organizationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ChangeOrganizationPlanCommand>
{
    public async Task<Result> Handle(ChangeOrganizationPlanCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) 
            return Result.Failure(OrganizationErrors.NotFound);

        var result = organization.ChangeSubscriptionPlan(request.SubscriptionPlanId, dateTimeProvider.UtcNow);
        if (result.IsFailure) 
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Admin.ChangeOrganizationPlan;

internal sealed class ChangeOrganizationPlanCommandHandler(
    IOrganizationRepository organizationRepository,
    ISubscriptionPlanLimitsProvider planLimitsProvider,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ChangeOrganizationPlanCommand>
{
    public async Task<Result> Handle(ChangeOrganizationPlanCommand request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null) 
            return Result.Failure(OrganizationErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;
        var result = organization.ChangeSubscriptionPlan(request.SubscriptionPlanId, utcNow);
        if (result.IsFailure) 
            return result;

        var limits = await planLimitsProvider.GetLimitsAsync(request.SubscriptionPlanId, cancellationToken);
        var limitResult = organization.SetMemberLimit(limits.OrganizationMemberLimit, utcNow);
        if (limitResult.IsFailure) 
            return limitResult;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}
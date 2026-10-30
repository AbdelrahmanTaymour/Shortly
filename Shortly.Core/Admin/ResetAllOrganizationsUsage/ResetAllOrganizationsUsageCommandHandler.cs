using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Admin.ResetAllOrganizationsUsage;

internal sealed class ResetAllOrganizationsUsageCommandHandler(IOrganizationUsageRepository organizationUsageRepository, IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ResetAllOrganizationsUsageCommand, int>
{
    public async Task<Result<int>> Handle(ResetAllOrganizationsUsageCommand request, CancellationToken cancellationToken)
        => await organizationUsageRepository.ResetAllMonthlyAsync(dateTimeProvider.UtcNow, cancellationToken);
}
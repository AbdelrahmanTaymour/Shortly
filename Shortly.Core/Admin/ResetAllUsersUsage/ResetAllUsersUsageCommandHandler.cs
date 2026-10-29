using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.ResetAllUsersUsage;

internal sealed class ResetAllUsersUsageCommandHandler(IUserUsageRepository userUsageRepository)
    : ICommandHandler<ResetAllUsersUsageCommand, int>
{
    public async Task<Result<int>> Handle(ResetAllUsersUsageCommand request, CancellationToken cancellationToken)
    {
        var affected = await userUsageRepository.ResetAllMonthlyAsync(cancellationToken);
        return affected;
    }
}
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.ResetUserUsage;

internal sealed class ResetUserUsageCommandHandler(
    IUserRepository userRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ResetUserUsageCommand>
{
    public async Task<Result> Handle(ResetUserUsageCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound);

        user.Usage.ResetMonthly(dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
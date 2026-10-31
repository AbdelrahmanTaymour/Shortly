using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.LockUser;

internal sealed class LockUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<LockUserCommand>
{
    public async Task<Result> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;
        var result = user.Security.LockManually(request.Reason, request.Duration, utcNow);
        if (result.IsFailure) return result;

        UserAuditLog.Record(
            user.Id, 
            "AdminLock", 
            $"Locked by {userContext.UserId}: {request.Reason}",
            null, 
            null, 
            utcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
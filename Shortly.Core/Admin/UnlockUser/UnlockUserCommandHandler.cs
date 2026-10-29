using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.UnlockUser;

internal sealed class UnlockUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<UnlockUserCommand>
{
    public async Task<Result> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;
        user.Security.ResetFailedLoginAttempts(utcNow);
        UserAuditLog.Record(user.Id, "AdminUnlock", $"Unlocked by {userContext.UserId}", null, null, utcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
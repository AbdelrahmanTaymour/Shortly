using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Users.DeleteAccount;

internal sealed class DeleteAccountCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IJwtService jwtService)
    : ICommandHandler<DeleteAccountCommand>
{
    public async Task<Result> Handle(DeleteAccountCommand delete, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound);

        var result = user.SoftDelete(userContext.UserId, dateTimeProvider.UtcNow);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await jwtService.RevokeAllForUserAsync(user.Id, cancellationToken);

        return Result.Success();
    }
}
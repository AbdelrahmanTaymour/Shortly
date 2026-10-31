using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
        if (user is null) 
            return Result.Failure(UserErrors.NotFound);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure(UserErrors.InvalidCredentials);

        var changeResult = user.ChangePassword(passwordHasher.Hash(request.NewPassword), dateTimeProvider.UtcNow);
        if (changeResult.IsFailure) 
            return changeResult;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await jwtService.RevokeAllForUserAsync(user.Id, cancellationToken);

        return Result.Success();
    }
}
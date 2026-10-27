using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure(UserErrors.InvalidCredentials);

        var newHash = passwordHasher.Hash(request.NewPassword);
        await userRepository.ChangePasswordAsync(user.Id, newHash, cancellationToken);

        await jwtService.RevokeAllForUserAsync(user.Id, cancellationToken);

        return Result.Success();
    }
}
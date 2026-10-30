using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IUserActionTokenRepository tokenRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Sha256Extensions.ComputeHash(request.Token);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (token is null) return Result.Failure(UserActionTokenErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;
        var validation = token.EnsureValidFor(enUserActionTokenType.PasswordReset, utcNow);
        if (validation.IsFailure) return validation;

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound);

        var changeResult = user.ChangePassword(passwordHasher.Hash(request.NewPassword), utcNow);
        if (changeResult.IsFailure) return changeResult;

        token.MarkUsed(utcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await jwtService.RevokeAllForUserAsync(user.Id, cancellationToken);
        return Result.Success();
    }
}
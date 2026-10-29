using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Tokens;

namespace Shortly.Core.Account.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IUserActionTokenRepository tokenRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Sha256Extensions.ComputeHash(request.Token);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null || token.TokenType != enUserActionTokenType.PasswordReset)
            return Result.Failure(UserActionTokenErrors.NotFound);

        if (token.Used)
            return Result.Failure(UserActionTokenErrors.Used);

        if (token.ExpiresAt < DateTime.UtcNow)
            return Result.Failure(UserActionTokenErrors.Expired);

        await tokenRepository.ConsumeTokenAsync(tokenHash, enUserActionTokenType.PasswordReset, cancellationToken);

        var newHash = passwordHasher.Hash(request.NewPassword);
        await userRepository.ChangePasswordAsync(token.UserId, newHash, cancellationToken);

        await jwtService.RevokeAllForUserAsync(token.UserId, cancellationToken);

        return Result.Success();
    }
}
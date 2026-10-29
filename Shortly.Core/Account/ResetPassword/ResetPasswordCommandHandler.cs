using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IUserActionTokenRepository tokenRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Sha256Extensions.ComputeHash(request.Token);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null || token.TokenType != enUserActionTokenType.PasswordReset)
            return Result.Failure(UserActionTokenErrors.NotFound);
        
        if (token.Used) 
            return Result.Failure(UserActionTokenErrors.Used);
        
        if (token.ExpiresAt < dateTimeProvider.UtcNow) 
            return Result.Failure(UserActionTokenErrors.Expired);

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null) 
            return Result.Failure(UserErrors.NotFound);

        var changeResult = user.ChangePassword(passwordHasher.Hash(request.NewPassword), dateTimeProvider.UtcNow);
        if (changeResult.IsFailure) return changeResult;

        await tokenRepository.ConsumeTokenAsync(tokenHash, enUserActionTokenType.PasswordReset, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        await jwtService.RevokeAllForUserAsync(user.Id, cancellationToken);

        return Result.Success();
    }
}
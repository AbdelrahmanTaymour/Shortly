using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.VerifyEmail;

internal sealed class VerifyEmailCommandHandler(
    IUserActionTokenRepository tokenRepository,
    IUserRepository userRepository,
    IJwtService jwtService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<VerifyEmailCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Sha256Extensions.ComputeHash(request.Token);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null || token.TokenType != enUserActionTokenType.EmailVerification)
            return Result.Failure<AccessTokenResponse>(UserActionTokenErrors.NotFound);

        if (token.Used)
            return Result.Failure<AccessTokenResponse>(UserActionTokenErrors.Used);

        if (token.ExpiresAt < DateTime.UtcNow)
            return Result.Failure<AccessTokenResponse>(UserActionTokenErrors.Expired);

        await tokenRepository.ConsumeTokenAsync(tokenHash, enUserActionTokenType.EmailVerification, cancellationToken);

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<AccessTokenResponse>(UserErrors.NotFound);

        var confirmResult = user.ConfirmEmail(dateTimeProvider.UtcNow);
        if (confirmResult.IsFailure) 
            return Result.Failure<AccessTokenResponse>(confirmResult.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await jwtService.IssueTokensAsync(user, cancellationToken);
    }
}
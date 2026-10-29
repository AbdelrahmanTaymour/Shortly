using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.VerifyEmail;

internal sealed class VerifyEmailCommandHandler(
    IUserActionTokenRepository tokenRepository,
    IUserRepository userRepository,
    IJwtService jwtService) : ICommandHandler<VerifyEmailCommand, AccessTokenResponse>
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
        await userRepository.MarkEmailAsConfirmedAsync(token.UserId, cancellationToken);

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<AccessTokenResponse>(UserErrors.NotFound);

        return await jwtService.IssueTokensAsync(user, cancellationToken);
    }
}
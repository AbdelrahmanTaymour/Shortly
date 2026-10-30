using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
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
        if (token is null) return Result.Failure<AccessTokenResponse>(UserActionTokenErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;
        var validation = token.EnsureValidFor(enUserActionTokenType.EmailVerification, utcNow);
        if (validation.IsFailure) return Result.Failure<AccessTokenResponse>(validation.Error);

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null) return Result.Failure<AccessTokenResponse>(UserErrors.NotFound);

        var confirmResult = user.ConfirmEmail(utcNow);
        if (confirmResult.IsFailure) return Result.Failure<AccessTokenResponse>(confirmResult.Error);

        token.MarkUsed(utcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await jwtService.IssueTokensAsync(user, cancellationToken);
    }
}
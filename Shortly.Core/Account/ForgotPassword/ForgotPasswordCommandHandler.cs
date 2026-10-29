using System.Security.Cryptography;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IUserActionTokenRepository tokenRepository,
    IEmailNotifier emailNotifier,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null) 
            return Result.Success();

        await tokenRepository.InvalidateUserTokensAsync(user.Id, enUserActionTokenType.PasswordReset, cancellationToken);

        var plainToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var utcNow = dateTimeProvider.UtcNow;

        await tokenRepository.CreateAsync(new UserActionToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = enUserActionTokenType.PasswordReset,
            TokenHash = Sha256Extensions.ComputeHash(plainToken),
            ExpiresAt = utcNow.AddHours(1),
            CreatedAt = utcNow
        }, cancellationToken);

        emailNotifier.EnqueuePasswordResetAsync(user.Email, user.Username, plainToken);

        return Result.Success();
    }
}
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Tokens;

namespace Shortly.Core.Account.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IUserActionTokenRepository tokenRepository,
    IEmailNotifier emailNotifier) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Success(); // never reveal whether the email exists

        await tokenRepository.InvalidateUserTokensAsync(user.Id, enUserActionTokenType.PasswordReset, cancellationToken);

        var plainToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        await tokenRepository.CreateAsync(new UserActionToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = enUserActionTokenType.PasswordReset,
            TokenHash = Sha256Extensions.ComputeHash(plainToken),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        emailNotifier.EnqueueSendPasswordResetAsync(user.Email, user.Username, plainToken);

        return Result.Success();
    }
}
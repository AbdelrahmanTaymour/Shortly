using Shortly.Core.Abstractions.Email;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.SendEmailVerification;

internal sealed class SendEmailVerificationCommandHandler(
    IUserRepository userRepository,
    IUserActionTokenRepository tokenRepository,
    IEmailNotifier emailNotifier) : ICommandHandler<SendEmailVerificationCommand>
{
    public async Task<Result> Handle(SendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return Result.Success(); // never reveal whether the email exists

        if (user.IsEmailConfirmed)
            return Result.Failure(UserErrors.EmailAlreadyConfirmed);

        await tokenRepository.InvalidateUserTokensAsync(user.Id, enUserActionTokenType.EmailVerification, cancellationToken);

        var plainToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        await tokenRepository.CreateAsync(new UserActionToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = enUserActionTokenType.EmailVerification,
            TokenHash = Sha256Extensions.ComputeHash(plainToken),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        emailNotifier.EnqueueSendEmailVerificationAsync(user.Email, user.Username, plainToken);

        return Result.Success();
    }
}
using System.Security.Cryptography;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.SendEmailVerification;

internal sealed class SendEmailVerificationCommandHandler(
    IUserRepository userRepository,
    IUserActionTokenRepository tokenRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IEmailNotifier emailNotifier) : ICommandHandler<SendEmailVerificationCommand>
{
    public async Task<Result> Handle(SendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email ?? userContext.Email;
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null) return Result.Success(); // never reveal whether the email exists

        if (user.IsEmailConfirmed) return Result.Failure(UserErrors.EmailAlreadyConfirmed);

        var utcNow = dateTimeProvider.UtcNow;
        await tokenRepository.InvalidateActiveTokensAsync(user.Id, enUserActionTokenType.EmailVerification, utcNow, cancellationToken);

        var plainToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var token = UserActionToken.IssueEmailVerification(user.Id, Sha256Extensions.ComputeHash(plainToken), utcNow);

        tokenRepository.Add(token);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        emailNotifier.EnqueueSendEmailVerificationAsync(user.Email, user.Username, plainToken);
        return Result.Success();
    }
}
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.InitiateEmailChange;

internal sealed class InitiateEmailChangeCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IUserActionTokenRepository tokenRepository,
    IPasswordHasher passwordHasher,
    IEmailNotifier notifier) : ICommandHandler<InitiateEmailChangeCommand>
{
    public async Task<Result> Handle(InitiateEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure(UserErrors.InvalidCredentials);

        if (await userRepository.EmailExistsAsync(request.NewEmail, cancellationToken))
            return Result.Failure(UserErrors.EmailOrUsernameNotUnique);

        await tokenRepository.InvalidateUserTokensAsync(user.Id, enUserActionTokenType.EmailChange, cancellationToken);

        var plainToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        await tokenRepository.CreateAsync(new UserActionToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = enUserActionTokenType.EmailChange,
            TokenHash = Sha256Extensions.ComputeHash(plainToken),
            NewEmail = request.NewEmail,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        notifier.EnqueueEmailChangeConfirmationAsync(request.NewEmail, user.Username, plainToken);

        return Result.Success();
    }
}
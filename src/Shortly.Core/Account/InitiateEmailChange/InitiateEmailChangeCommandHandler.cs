using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.InitiateEmailChange;

internal sealed class InitiateEmailChangeCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IUserActionTokenRepository tokenRepository,
    IPasswordHasher passwordHasher,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IEmailNotifier notifier) : ICommandHandler<InitiateEmailChangeCommand>
{
    public async Task<Result> Handle(InitiateEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure(UserErrors.InvalidCredentials);

        if (await userRepository.EmailExistsAsync(request.NewEmail, cancellationToken))
            return Result.Failure(UserErrors.EmailOrUsernameNotUnique);

        var utcNow = dateTimeProvider.UtcNow;
        await tokenRepository.InvalidateActiveTokensAsync(user.Id, enUserActionTokenType.EmailChange, utcNow, cancellationToken);

        var plainToken = tokenHasher.GenerateToken();
        var tokenResult = UserActionToken.IssueEmailChange(user.Id, tokenHasher.Hash(plainToken), request.NewEmail, utcNow);
        if (tokenResult.IsFailure) return Result.Failure(tokenResult.Error);

        tokenRepository.Add(tokenResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        notifier.EnqueueEmailChangeConfirmationAsync(request.NewEmail, user.Username, plainToken);
        return Result.Success();
    }
}
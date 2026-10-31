using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.ConfirmEmailChange;

internal sealed class ConfirmEmailChangeCommandHandler(
    IUserActionTokenRepository tokenRepository,
    IUserRepository userRepository,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ConfirmEmailChangeCommand>
{
    public async Task<Result> Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenHasher.Hash(request.Token);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (token?.NewEmail is null) return Result.Failure(UserActionTokenErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;
        var validation = token.EnsureValidFor(enUserActionTokenType.EmailChange, utcNow);
        if (validation.IsFailure) return validation;

        if (await userRepository.EmailExistsAsync(token.NewEmail, cancellationToken))
            return Result.Failure(UserErrors.EmailOrUsernameNotUnique);

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound);

        var changeResult = user.ChangeEmail(token.NewEmail, utcNow);
        if (changeResult.IsFailure) return changeResult;

        token.MarkUsed(utcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
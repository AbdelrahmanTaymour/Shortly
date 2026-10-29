using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.ConfirmEmailChange;

internal sealed class ConfirmEmailChangeCommandHandler(
    IUserActionTokenRepository tokenRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ConfirmEmailChangeCommand>
{
    public async Task<Result> Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Sha256Extensions.ComputeHash(request.Token);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null || token.TokenType != enUserActionTokenType.EmailChange || token.NewEmail is null)
            return Result.Failure(UserActionTokenErrors.NotFound);
        
        if (token.Used) 
            return Result.Failure(UserActionTokenErrors.Used);
        
        if (token.ExpiresAt < dateTimeProvider.UtcNow) 
            return Result.Failure(UserActionTokenErrors.Expired);

        if (await userRepository.EmailExistsAsync(token.NewEmail, cancellationToken))
            return Result.Failure(UserErrors.EmailOrUsernameNotUnique); // someone claimed it between request and confirm

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null) 
            return Result.Failure(UserErrors.NotFound);

        var changeResult = user.ChangeEmail(token.NewEmail, dateTimeProvider.UtcNow);
        if (changeResult.IsFailure) 
            return changeResult;

        await tokenRepository.ConsumeTokenAsync(tokenHash, enUserActionTokenType.EmailChange, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
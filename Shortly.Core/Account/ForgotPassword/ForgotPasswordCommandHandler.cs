using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IUserActionTokenRepository tokenRepository,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork,
    IEmailNotifier emailNotifier,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null) return Result.Success();

        var utcNow = dateTimeProvider.UtcNow;
        await tokenRepository.InvalidateActiveTokensAsync(user.Id, enUserActionTokenType.PasswordReset, utcNow, cancellationToken);

        var plainToken = tokenHasher.GenerateToken();
        var token = UserActionToken.IssuePasswordReset(user.Id, tokenHasher.Hash(plainToken), utcNow);

        tokenRepository.Add(token);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        emailNotifier.EnqueuePasswordResetAsync(user.Email, user.Username, plainToken);
        return Result.Success();
    }
}
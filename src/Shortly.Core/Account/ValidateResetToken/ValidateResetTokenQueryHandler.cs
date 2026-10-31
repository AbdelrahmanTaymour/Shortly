using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;

namespace Shortly.Core.Account.ValidateResetToken;

internal sealed class ValidateResetTokenQueryHandler(
    IUserActionTokenRepository tokenRepository, 
    ITokenHasher tokenHasher, 
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<ValidateResetTokenQuery, bool>
{
    public async Task<Result<bool>> Handle(ValidateResetTokenQuery request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenHasher.Hash(request.Token);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        var isValid = token is not null
                      && token.TokenType == enUserActionTokenType.PasswordReset
                      && !token.Used
                      && token.ExpiresAt >= dateTimeProvider.UtcNow;

        return Result.Success(isValid);
    }
}
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Extensions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;

namespace Shortly.Core.Account.ValidateResetToken;

internal sealed class ValidateResetTokenQueryHandler(IUserActionTokenRepository tokenRepository)
    : IQueryHandler<ValidateResetTokenQuery, bool>
{
    public async Task<Result<bool>> Handle(ValidateResetTokenQuery request, CancellationToken cancellationToken)
    {
        var tokenHash = Sha256Extensions.ComputeHash(request.Token);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        var isValid = token is not null
                      && token.TokenType == enUserActionTokenType.PasswordReset
                      && !token.Used
                      && token.ExpiresAt >= DateTime.UtcNow;

        return Result.Success(isValid);
    }
}
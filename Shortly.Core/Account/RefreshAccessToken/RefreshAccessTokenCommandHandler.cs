using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.RefreshAccessToken;

internal sealed class RefreshAccessTokenCommandHandler(
    IJwtService jwtService,
    ICookieService cookieService) : ICommandHandler<RefreshAccessTokenCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = cookieService.GetRefreshToken();
        if (string.IsNullOrEmpty(refreshToken))
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidRefreshToken);

        var result = await jwtService.RefreshAccessTokenAsync(refreshToken, cancellationToken);
        if (result.IsFailure) return result;

        cookieService.SetRefreshToken(result.Value.RefreshToken, DateTime.UtcNow.AddDays(15));

        return result;
    }
}
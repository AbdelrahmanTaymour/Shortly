using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.RefreshAccessToken;

internal sealed class RefreshAccessTokenCommandHandler(
    IJwtService jwtService,
    ICookieService cookieService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RefreshAccessTokenCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = cookieService.GetRefreshToken();
        if (string.IsNullOrEmpty(refreshToken))
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.InvalidRefreshToken);

        var result = await jwtService.RefreshAccessTokenAsync(refreshToken, cancellationToken);
        if (result.IsFailure) return result;

        var utcNow = dateTimeProvider.UtcNow;
        cookieService.SetRefreshToken(result.Value.RefreshToken, utcNow.Add(jwtService.RefreshTokenLifetime));

        return result;
    }
}
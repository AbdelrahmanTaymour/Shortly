using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.GoogleLogin;

internal sealed class GoogleLoginCommandHandler(
    IUserRepository userRepository,
    IJwtService jwtService,
    ICookieService cookieService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<GoogleLoginCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        
        var user = await userRepository.GetByGoogleIdAsync(request.GoogleId, cancellationToken)
                   ?? await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            var registration = User.RegisterOAuth(request.Email, request.GoogleId, request.Picture, utcNow);
            if (registration.IsFailure) return Result.Failure<AccessTokenResponse>(registration.Error);

            user = registration.Value;
            user.Profile.UpdateName(request.Name, utcNow);
            userRepository.Add(user);
        }

        var tokensResult = await jwtService.IssueTokensAsync(user, cancellationToken);
        if (tokensResult.IsFailure) return tokensResult;

        cookieService.SetRefreshToken(tokensResult.Value.RefreshToken, utcNow.Add(jwtService.RefreshTokenLifetime));
        
        return tokensResult;
    }
}
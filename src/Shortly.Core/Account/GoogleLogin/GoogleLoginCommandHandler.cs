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
        var user = await userRepository.GetByGoogleIdAsync(request.GoogleId, cancellationToken)
                   ?? await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            user = User.RegisterOAuth(request.Email, request.GoogleId, request.Picture, dateTimeProvider.UtcNow);
            userRepository.Add(user);
        }

        var tokensResult = await jwtService.IssueTokensAsync(user, cancellationToken);
        if (tokensResult.IsFailure) return tokensResult;

        var utcNow = dateTimeProvider.UtcNow;
        cookieService.SetRefreshToken(tokensResult.Value.RefreshToken, utcNow.AddDays(15));
        
        return tokensResult;
    }
}
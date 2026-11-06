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
        
        var user = await userRepository.GetByGoogleIdAsync(request.GoogleId, cancellationToken);

        if (user is null)
        {
            var sameEmail = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (sameEmail is null)
            {
                // Register a new OAuth-backed user record
                var registration = User.RegisterOAuth(request.Email, request.GoogleId, request.Picture, utcNow);
                if (registration.IsFailure) 
                    return Result.Failure<AccessTokenResponse>(registration.Error);

                user = registration.Value;
                user.Profile.UpdateName(request.Name, utcNow);
                userRepository.Add(user);
            }
            else
            {
                // Prevent account takeover: only confirmed accounts may attach a Google identity
                if (!sameEmail.IsEmailConfirmed)
                    return Result.Failure<AccessTokenResponse>(UserErrors.GoogleLinkRequiresConfirmedEmail);

                var linked = sameEmail.LinkGoogleAccount(request.GoogleId, request.Picture, utcNow);
                if (linked.IsFailure) 
                    return Result.Failure<AccessTokenResponse>(linked.Error);

                user = sameEmail;
            }
        }

        // Verify account state (fails if deactivated, locked, or soft-deleted)
        var loginResult = user.RecordSuccessfulLogin(utcNow);
        if (loginResult.IsFailure) 
            return Result.Failure<AccessTokenResponse>(loginResult.Error);

        // Issue JWT access/refresh token pair
        var tokensResult = await jwtService.IssueTokensAsync(user, cancellationToken);
        if (tokensResult.IsFailure) 
            return tokensResult;

        // Persist refresh token in HTTP-only cookie
        cookieService.SetRefreshToken(tokensResult.Value.RefreshToken, utcNow.Add(jwtService.RefreshTokenLifetime));
        
        return tokensResult;
    }
}
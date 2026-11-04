using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    ICookieService cookieService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LoginCommand, AccessTokenResponse>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    
    // Static dummy hash used to neutralize timing attacks for non-existent emails
    private static string? _dummyHash;

    public async Task<Result<AccessTokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        var utcNow = dateTimeProvider.UtcNow;

        // If the user does not exist, perform a dummy password verification to maintain identical response timing
        if (user is null)
        {
            _dummyHash ??= passwordHasher.Hash(Guid.NewGuid().ToString("N"));
            passwordHasher.Verify(request.Password, _dummyHash);
            
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }
        
        // Validate password credentials; record a failed attempt if verification fails
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await userRepository.RecordFailedLoginAsync(user.Id, utcNow, MaxFailedAttempts, LockoutDuration, cancellationToken);
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        // Check domain rules for successful login (e.g., account lockouts or status checks)
        var loginResult = user.RecordSuccessfulLogin(utcNow);
        if (loginResult.IsFailure) 
            return Result.Failure<AccessTokenResponse>(loginResult.Error);

        // Mint a secure access and refresh token pair
        var tokensResult = await jwtService.IssueTokensAsync(user, cancellationToken);
        if (tokensResult.IsFailure) 
            return tokensResult;

        // Securely set the refresh token inside an HTTP-only cookie
        cookieService.SetRefreshToken(tokensResult.Value.RefreshToken, utcNow.Add(jwtService.RefreshTokenLifetime));
        
        return tokensResult;
    }
}
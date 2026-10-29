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
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LoginCommand, AccessTokenResponse>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<Result<AccessTokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        var utcNow = dateTimeProvider.UtcNow;

        if (user is null) 
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(utcNow, MaxFailedAttempts, LockoutDuration);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        var loginResult = user.RecordSuccessfulLogin(utcNow);
        if (loginResult.IsFailure) 
            return Result.Failure<AccessTokenResponse>(loginResult.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tokensResult = await jwtService.IssueTokensAsync(user, cancellationToken);
        if (tokensResult.IsFailure) 
            return tokensResult;

        cookieService.SetRefreshToken(tokensResult.Value.RefreshToken, utcNow.AddDays(15));
        return tokensResult;
    }
}
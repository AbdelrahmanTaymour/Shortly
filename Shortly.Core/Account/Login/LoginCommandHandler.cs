using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Security;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IUserSecurityRepository userSecurityRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    ICookieService cookieService) : ICommandHandler<LoginCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null)
                await userSecurityRepository.IncrementFailedLoginAttemptsAsync(user.Id, cancellationToken);

            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        var tokensResult = await jwtService.IssueTokensAsync(user, cancellationToken);
        if (tokensResult.IsFailure) return tokensResult;

        cookieService.SetRefreshToken(tokensResult.Value.RefreshToken, DateTime.UtcNow.AddDays(15));

        return tokensResult;
    }
}
using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Authentication;

internal sealed class AuthenticationService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : IAuthenticationService
{
    public async Task<Result<Guid>> RegisterAsync(string email, string username, string password, CancellationToken cancellationToken = default)
    {
        if (await userRepository.EmailOrUsernameExistsAsync(email, username, cancellationToken))
            return Result.Failure<Guid>(AuthenticationErrors.EmailNotUnique);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            PasswordHash = passwordHasher.Hash(password),
            Role = UserRole.RegisteredUser,
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.CreateAsync(user); // already creates Profile/Security/Usage rows transactionally

        return user.Id;
    }

    public async Task<Result<AccessTokenResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.InvalidCredentials);

        return await jwtService.IssueTokensAsync(user, cancellationToken);
    }

    public Task<Result<AccessTokenResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        jwtService.RefreshAccessTokenAsync(refreshToken, cancellationToken);

    public Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        jwtService.RevokeAsync(refreshToken, cancellationToken);
}
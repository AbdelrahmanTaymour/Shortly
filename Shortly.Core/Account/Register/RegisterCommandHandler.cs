using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    ICookieService cookieService) : ICommandHandler<RegisterCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.EmailOrUsernameExistsAsync(request.Email, request.Username, cancellationToken))
            return Result.Failure<AccessTokenResponse>(UserErrors.EmailOrUsernameNotUnique);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Username = request.Username,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRole.RegisteredUser,
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.CreateAsync(user);

        var tokensResult = await jwtService.IssueTokensAsync(user, cancellationToken);
        if (tokensResult.IsFailure) return tokensResult;

        cookieService.SetRefreshToken(tokensResult.Value.RefreshToken, DateTime.UtcNow.AddDays(15));

        return tokensResult;
    }
}
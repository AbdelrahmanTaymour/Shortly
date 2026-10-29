using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.GoogleLogin;

internal sealed class GoogleLoginCommandHandler(
    IUserRepository userRepository,
    IJwtService jwtService,
    ICookieService cookieService) : ICommandHandler<GoogleLoginCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetUserByGoogleIdAsync(request.GoogleId, cancellationToken)
                   ?? await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Username = request.Email,
                PasswordHash = string.Empty,
                GoogleId = request.GoogleId,
                GoogleProfilePicture = request.Picture,
                IsOAuthUser = true,
                IsEmailConfirmed = true,
                Role = UserRole.RegisteredUser,
                CreatedAt = DateTime.UtcNow
            };

            await userRepository.CreateAsync(user);
        }

        var tokensResult = await jwtService.IssueTokensAsync(user, cancellationToken);
        if (tokensResult.IsFailure) return tokensResult;

        cookieService.SetRefreshToken(tokensResult.Value.RefreshToken, DateTime.UtcNow.AddDays(15));

        return tokensResult;
    }
}
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Abstractions.Authentication;

public interface IAuthenticationService
{
    Task<Result<Guid>> RegisterAsync(string email, string username, string password, CancellationToken cancellationToken = default);

    Task<Result<AccessTokenResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<Result<AccessTokenResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
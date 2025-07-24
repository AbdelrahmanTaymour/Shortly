using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.DTOs.ValidationDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.ServiceContracts;

public interface IAuthenticationService
{
    Task<AuthenticationResponse?> Login(LoginRequest loginRequest);
    Task<AuthenticationResponse?> Register(RegisterRequest registerRequest);
    Task<TokenResponse> GenerateTokensAsync(User user);
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken, bool extendExpiry);
    TokenValidationResultDto ValidateToken(string token, bool validateLifetime = true);
    Task RevokeTokenAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(Guid userId);
}
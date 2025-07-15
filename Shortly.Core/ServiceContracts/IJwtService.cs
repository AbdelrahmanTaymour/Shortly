using Microsoft.IdentityModel.Tokens;
using Shortly.Core.DTOs;
using Shortly.Core.Entities;

namespace Shortly.Core.ServiceContracts;

public interface IJwtService
{
    Task<TokenResponse> GenerateTokensAsync(User user);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    Task<TokenValidationResulDto> ValidateTokenAsync(string token, bool validateLifetime = true);
    Task RevokeTokenAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(Guid userId);
}
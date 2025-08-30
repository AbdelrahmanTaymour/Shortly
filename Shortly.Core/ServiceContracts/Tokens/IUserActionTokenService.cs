using Shortly.Core.DTOs.UserActionTokensDTOs;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts.Tokens;

public interface IUserActionTokenService
{
    Task<UserActionTokenDto> GenerateTokenAsync(Guid userId, enUserActionTokenType tokenType, TimeSpan? customExpiration = null, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string token, enUserActionTokenType expectedTokenType, CancellationToken cancellationToken = default);
    Task<UserActionTokenDto> GetTokenDetailsAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> ConsumeTokenAsync(string token, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
    Task<bool> InvalidateUserTokensAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
    Task<bool> HasActiveTokenAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
}
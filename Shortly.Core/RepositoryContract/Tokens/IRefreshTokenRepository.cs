using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.Tokens;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken?> GetActiveRefreshTokenByUserIdAsync(Guid userId);
    Task<IEnumerable<RefreshToken>> GetAllActiveRefreshTokensByUserIdAsync(Guid userId);
    Task<RefreshToken?> AddRefreshTokenAsync(RefreshToken refreshToken);
    Task<bool> UpdateRefreshTokenAsync(RefreshToken refreshToken);
    Task<bool> ResetRefreshTokenAsync(Guid id, string newRefreshToken, DateTime expiresAt);
    Task RemoveRefreshTokenAsync(RefreshToken refreshToken);
    Task<bool> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsRefreshTokenActiveAsync(string token);
    Task<int> CleanupExpiredTokensAsync();
}
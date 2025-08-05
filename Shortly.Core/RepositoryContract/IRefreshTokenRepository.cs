using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken?> GetActiveRefreshTokenByUserIdAsync(Guid userId);
    Task<User?> GetUserFromRefreshTokenAsync(Guid userId);
    Task<List<RefreshToken>?> GetAllActiveRefreshTokensByUserIdAsync(Guid userId);
    Task<RefreshToken?> AddRefreshTokenAsync(RefreshToken refreshToken);
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
    Task RemoveRefreshTokenAsync(RefreshToken refreshToken);
    Task<bool> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> IsRefreshTokenActiveAsync(string token);
    Task CleanupExpiredTokensAsync();
    Task SaveChangesAsync();
}
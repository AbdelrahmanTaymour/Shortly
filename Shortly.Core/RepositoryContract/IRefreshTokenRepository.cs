using Shortly.Core.Entities;

namespace Shortly.Core.RepositoryContract;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> AddRefreshTokenAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken?> GetActiveRefreshTokenByUserIdAsync(Guid userId);
    Task<List<RefreshToken>?> GetAllActiveRefreshTokensByUserIdAsync(Guid userId);
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
    Task<User?> GetUserFromRefreshTokenAsync(Guid userId);
    Task<bool> IsRefreshTokenActiveAsync(string token);
    Task CleanupExpiredTokensAsync();
    Task SaveChangesAsync();
}
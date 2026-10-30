namespace Shortly.Domain.RefreshToken;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    void Add(RefreshToken refreshToken);
    Task<bool> RotateAsync(Guid id, string newTokenHash, TimeSpan lifetime, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(string tokenHash, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> RevokeAllAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
namespace Shortly.Domain.RefreshToken;

/// <summary>Defines data access operations for secure refresh token lifecycle management.</summary>
public interface IRefreshTokenRepository
{
    /// <summary>Retrieves a refresh token record using its cryptographic hash.</summary>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Stages a new refresh token record for persistence.</summary>
    void Add(RefreshToken refreshToken);

    /// <summary>
    /// Performs an atomic compare-and-swap rotation: replaces the token only if it matches <paramref name="currentTokenHash"/> 
    /// and is neither revoked nor expired. Returns false if a concurrent request already rotated or revoked it.
    /// </summary>
    Task<bool> RotateAsync(Guid id, string currentTokenHash, string newTokenHash, TimeSpan lifetime, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Revokes a specific refresh token by its hash.</summary>
    Task<bool> RevokeAsync(string tokenHash, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Revokes all active refresh tokens associated with a specific user.</summary>
    Task<int> RevokeAllAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Removes expired or invalidated refresh tokens from the database for maintenance.</summary>
    Task<int> CleanupExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
using Microsoft.EntityFrameworkCore;
using Shortly.Domain.RefreshToken;

namespace Shortly.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(ApplicationDbContext dbContext) 
    : Repository<RefreshToken, Guid>(dbContext), IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => await DbContext.Set<RefreshToken>()
            .AsNoTracking()
            .Include(rt => rt.User).ThenInclude(u => u.Profile) // only if the caller actually needs Profile — see note below
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> RotateAsync(Guid id, string newTokenHash, TimeSpan lifetime, DateTime utcNow, CancellationToken cancellationToken = default)
        => await DbContext.Set<RefreshToken>()
            .Where(rt => rt.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.TokenHash, newTokenHash)
                .SetProperty(rt => rt.ExpiresAt, utcNow.Add(lifetime))
                .SetProperty(rt => rt.UpdatedAt, utcNow), cancellationToken) > 0;

    public async Task<bool> RevokeAsync(string tokenHash, DateTime utcNow, CancellationToken cancellationToken = default)
        => await DbContext.Set<RefreshToken>()
            .Where(rt => rt.TokenHash == tokenHash && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, utcNow), cancellationToken) > 0;

    public async Task<int> RevokeAllAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
        => await DbContext.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, utcNow), cancellationToken);

    public async Task<int> CleanupExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        => await DbContext.Set<RefreshToken>()
            .Where(rt => rt.ExpiresAt <= utcNow)
            .ExecuteDeleteAsync(cancellationToken);
}
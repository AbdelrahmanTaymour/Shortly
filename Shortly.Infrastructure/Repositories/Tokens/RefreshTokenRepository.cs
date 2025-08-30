using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;
using Shortly.Core.RepositoryContract.Tokens;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.Tokens;

public class RefreshTokenRepository(SQLServerDbContext dbContext, ILogger<RefreshTokenRepository> logger) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        var refreshTokenEntity = await dbContext.RefreshTokens.AddAsync(refreshToken);
        await dbContext.SaveChangesAsync();
        return refreshTokenEntity.Entity;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await dbContext.RefreshTokens
            .AsNoTracking()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == token);
    }

    public async Task<RefreshToken?> GetActiveRefreshTokenByUserIdAsync(Guid userId)
    {
        return await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<RefreshToken>> GetAllActiveRefreshTokensByUserIdAsync(Guid userId)
    {
        return await dbContext.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateRefreshTokenAsync(RefreshToken refreshToken)
    {
        dbContext.RefreshTokens.Update(refreshToken);
        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> ResetRefreshTokenAsync(Guid id, string newRefreshToken, DateTime expiresAt)
    {
        return await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.Id == id)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(rt => rt.TokenHash, newRefreshToken)
                    .SetProperty(rt => rt.ExpiresAt, expiresAt)) > 0;
    }

    public async Task RemoveRefreshTokenAsync(RefreshToken refreshToken)
    {
        dbContext.RefreshTokens.Remove(refreshToken);
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(rf => rf.TokenHash == token && !rf.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(rt => rt.IsRevoked, true)
                    .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow)
                , cancellationToken) > 0;
    }

    public async Task<bool> RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rawAffected = await dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(rt => rt.IsRevoked, true)
                        .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow)
                    , cancellationToken);
            return rawAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting all tokens for user: {UserId}", userId);
            throw new DatabaseException("Failed to delete user", ex);
        }
    }
    public async Task<bool> IsRefreshTokenActiveAsync(string token)
    {
        return await dbContext.RefreshTokens
            .AnyAsync(rt => rt.TokenHash == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
       return await dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAt <= DateTime.UtcNow)
            .ExecuteDeleteAsync();
    }
}
using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories;

public class RefreshTokenRepository(SQLServerDbContext dbContext):IRefreshTokenRepository
{
    private readonly SQLServerDbContext _dbContext = dbContext;
    public async Task<RefreshToken?> AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        var refreshTokenEntity = await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();
        return refreshTokenEntity.Entity;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<RefreshToken?> GetActiveRefreshTokenByUserIdAsync(Guid userId)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .FirstOrDefaultAsync();

        return refreshToken;
    }

    public async Task<List<RefreshToken>?> GetAllActiveRefreshTokensByUserIdAsync(Guid userId)
    {
        var refreshTokens = await _dbContext.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
        
        return refreshTokens;
    }

    public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
    {
        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<User?> GetUserFromRefreshTokenAsync(Guid userId)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> IsRefreshTokenActiveAsync(string token)
    {
        return await _dbContext.RefreshTokens
            .AnyAsync(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);
    }

    public async Task CleanupExpiredTokensAsync()
    {
        await _dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAt <= DateTime.UtcNow)
            .ExecuteDeleteAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
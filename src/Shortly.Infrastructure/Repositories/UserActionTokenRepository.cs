using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Tokens;

namespace Shortly.Infrastructure.Repositories;

public sealed class UserActionTokenRepository(ApplicationDbContext dbContext) 
    : Repository<UserActionToken, Guid>(dbContext), IUserActionTokenRepository
{
    public async Task<UserActionToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => await DbContext.Set<UserActionToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<int> InvalidateActiveTokensAsync(Guid userId, enUserActionTokenType tokenType, DateTime utcNow, CancellationToken cancellationToken = default)
        => await DbContext.Set<UserActionToken>()
            .Where(t => t.UserId == userId && t.TokenType == tokenType && !t.Used)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Used, true)
                .SetProperty(t => t.UsedAt, utcNow), cancellationToken);

    public async Task<int> DeleteExpiredOrUsedAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        => await DbContext.Set<UserActionToken>()
            .Where(t => t.Used || t.ExpiresAt <= utcNow)
            .ExecuteDeleteAsync(cancellationToken);
}
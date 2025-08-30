using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.RepositoryContract.Tokens;

public interface IUserActionTokenRepository
{
    Task<UserActionToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserActionToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<UserActionToken?> GetActiveTokenAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserActionToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserActionToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default);
    Task<UserActionToken> CreateAsync(UserActionToken token, CancellationToken cancellationToken = default);
    Task<UserActionToken> UpdateAsync(UserActionToken token, CancellationToken cancellationToken = default);
    Task<bool> ConsumeTokenAsync(string tokenHash, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
    Task<bool> InvalidateUserTokensAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
    Task<bool> HasActiveTokenAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken);
}
namespace Shortly.Domain.Tokens;

public interface IUserActionTokenRepository
{
    Task<UserActionToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    void Add(UserActionToken token);
    Task<int> InvalidateActiveTokensAsync(Guid userId, enUserActionTokenType tokenType, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredOrUsedAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
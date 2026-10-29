
namespace Shortly.Domain.ShortUrls;

public interface IShortUrlRepository
{
    Task<ShortUrl?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ShortUrl?> GetByShortCodeAsync(ShortCode shortCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShortUrl>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task<bool> ExistsByShortCodeAsync(ShortCode shortCode, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetExistingShortCodesAsync(IReadOnlyCollection<ShortCode> shortCodes, CancellationToken cancellationToken = default);

    Task<bool> ExistsAnyForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> DetachAllFromUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    void Add(ShortUrl shortUrl);
    void Remove(ShortUrl shortUrl);
}
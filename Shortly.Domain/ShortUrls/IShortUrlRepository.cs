
namespace Shortly.Domain.ShortUrls;

public interface IShortUrlRepository
{
    Task<ShortUrl?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShortUrl>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task<bool> ExistsByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetExistingShortCodesAsync(IReadOnlyCollection<string> shortCodes, CancellationToken cancellationToken = default);

    void Add(ShortUrl shortUrl);
    void Remove(ShortUrl shortUrl);
}
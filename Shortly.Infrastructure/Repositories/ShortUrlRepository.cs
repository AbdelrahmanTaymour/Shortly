using Microsoft.EntityFrameworkCore;
using Shortly.Domain.ShortUrls;

namespace Shortly.Infrastructure.Repositories;

/// <summary>
///     Repository class for managing database operations related to the <see cref="ShortUrl" /> entity.
///     This class implements the <see cref="IShortUrlRepository" /> interface and provides
///     CRUD operations for the ShortUrl database model.
/// </summary>
internal sealed class ShortUrlRepository(ApplicationDbContext dbContext)
    : Repository<ShortUrl, long>(dbContext), IShortUrlRepository
{
    public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<ShortUrl>()
            .FirstOrDefaultAsync(s => s.ShortCode.Value == shortCode, cancellationToken);
    }

    public async Task<IReadOnlyList<ShortUrl>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<ShortUrl>()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<ShortUrl>()
            .AnyAsync(s => s.ShortCode.Value == shortCode, cancellationToken);
    }

    public async Task<HashSet<string>> GetExistingShortCodesAsync(
        IReadOnlyCollection<string> shortCodes,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<ShortUrl>()
            .Where(s => shortCodes.Contains(s.ShortCode.Value))
            .Select(s => s.ShortCode.Value)
            .ToHashSetAsync(cancellationToken);
    }
}
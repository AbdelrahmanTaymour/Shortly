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
    public async Task<ShortUrl?> GetByShortCodeAsync(ShortCode shortCode, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<ShortUrl>()
            .FirstOrDefaultAsync(s => s.ShortCode == shortCode, cancellationToken);
    }

    public async Task<IReadOnlyList<ShortUrl>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return [];

        return await DbContext.Set<ShortUrl>()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByShortCodeAsync(ShortCode shortCode, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<ShortUrl>()
            .AnyAsync(s => s.ShortCode == shortCode, cancellationToken);
    }
    
    public async Task<HashSet<string>> GetExistingShortCodesAsync(
        IReadOnlyCollection<ShortCode> shortCodes,
        CancellationToken cancellationToken = default)
    {
        if (shortCodes.Count == 0) return [];

        var existing = await DbContext.Set<ShortUrl>()
            .Where(s => shortCodes.Contains(s.ShortCode))
            .Select(s => s.ShortCode)
            .ToListAsync(cancellationToken);

        return [.. existing.Select(c => c.Value)];
    }

    public async Task<bool> ExistsAnyForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbContext.Set<ShortUrl>().AnyAsync(s => s.UserId == userId, cancellationToken);

    public async Task<int> DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbContext.Set<ShortUrl>().Where(s => s.UserId == userId).ExecuteDeleteAsync(cancellationToken);

    public async Task<int> DetachAllFromUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbContext.Set<ShortUrl>().Where(s => s.UserId == userId).ExecuteUpdateAsync(
            s => s.SetProperty(x => x.UserId, (Guid?)null), cancellationToken);
}
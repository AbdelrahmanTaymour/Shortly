using Microsoft.EntityFrameworkCore;
using Shortly.Core.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories;

/// <summary>
/// Repository class for managing database operations related to the <see cref="ShortUrl"/> entity.
/// This class implements the <see cref="IShortUrlRepository"/> interface and provides
/// CRUD operations for the ShortUrl database model.
/// </summary>
internal class ShortUrlRepository(SQLServerDbContext dbContext) : IShortUrlRepository
{
    private readonly SQLServerDbContext _dbContext = dbContext;

    
    public async Task<List<ShortUrl>> GetAllAsync()
    {
        return await _dbContext.ShortUrls.ToListAsync();
    }

    
    public async Task<ShortUrl?> GetShortUrlByIdAsync(Guid id)
    {
        return await _dbContext.ShortUrls.FindAsync(id);
    }

    
    public async Task<ShortUrl?> GetShortUrlByShortCodeAsync(string shortCode)
    {
        return await _dbContext.ShortUrls.FirstOrDefaultAsync(url => url.ShortCode == shortCode);
    }

    
    public async Task<ShortUrl?> CreateShortUrlAsync(ShortUrl shortUrl)
    {
        var entity = await _dbContext.ShortUrls.AddAsync(shortUrl);
        await _dbContext.SaveChangesAsync();
        return entity.Entity;
    }

    /// <summary>
    /// Updates an existing <see cref="ShortUrl"/> entity by its unique identifier.
    /// Only the <see cref="ShortUrl.OriginalUrl"/> and <see cref="ShortUrl.ShortCode"/> properties are updated while other properties remain unchanged.
    /// </summary>
    /// <param name="id">The unique identifier of the <see cref="ShortUrl"/> to update.</param>
    /// <param name="updatedShortUrl">
    /// The updated <see cref="ShortUrl"/> object containing values for <see cref="ShortUrl.OriginalUrl"/> and <see cref="ShortUrl.ShortCode"/>.
    /// </param>
    /// <returns>The updated <see cref="ShortUrl"/> instance if found and updated, otherwise null.</returns>
    public async Task<ShortUrl?> UpdateShortUrlByIdAsync(Guid id, ShortUrl updatedShortUrl)
    {
        var existingUrl = await _dbContext.ShortUrls.FindAsync(id);
        if (existingUrl is null)
        {
            return null;
        }
        
        existingUrl.OriginalUrl = updatedShortUrl.OriginalUrl;
        existingUrl.ShortCode = updatedShortUrl.ShortCode;
        await _dbContext.SaveChangesAsync();
        
        return existingUrl;
    }

    
    public async Task<ShortUrl?> DeleteByIdAsync(Guid id)
    {
        var existingUrl = await _dbContext.ShortUrls.FindAsync(id);
        if (existingUrl is null)
        {
            return null;
        }
        
        _dbContext.ShortUrls.Remove(existingUrl);
        await _dbContext.SaveChangesAsync();
        
        return existingUrl;
    }

   
    public async Task<ShortUrl?> DeleteByShortCodeAsync(string shortCode)
    {
        var existingUrl = await _dbContext.ShortUrls.FirstOrDefaultAsync(url => url.ShortCode == shortCode);
        if (existingUrl is null)
        {
            return null;
        }
        
        _dbContext.ShortUrls.Remove(existingUrl);
        await _dbContext.SaveChangesAsync();
        
        return existingUrl;
    }

    
    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task IncrementAccessCountAsync(string shortCode)
    {
        await _dbContext.ShortUrls
            .Where(s => s.ShortCode == shortCode)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.AccessCount, x => x.AccessCount + 1));
    }

    public async Task<bool> ShortCodeExistsAsync(string shortCode)
    {
        return await _dbContext.ShortUrls.AnyAsync(s => s.ShortCode == shortCode);
    }

    public async Task<int> GetShortUrlCountAsync()
    {
        return await _dbContext.ShortUrls.CountAsync();
    }
}
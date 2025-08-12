using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UrlManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UrlManagement;

/// <summary>
///     Repository class for managing database operations related to the <see cref="ShortUrl" /> entity.
///     This class implements the <see cref="IShortUrlRepository" /> interface and provides
///     CRUD operations for the ShortUrl database model.
/// </summary>
public class ShortUrlRepository(SQLServerDbContext dbContext, ILogger<ShortUrlRepository> logger) : IShortUrlRepository
{
    /// <inheritdoc />
    public async Task<ShortUrl?> GetByIdAsync(long id, bool includeTracking = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = includeTracking ? dbContext.ShortUrls : dbContext.ShortUrls.AsNoTracking();
            return await query
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving short URL by ID: {Id}", id);
            throw new DatabaseException("Failed to retrieve short URL by ID", ex);
        }
    }


    /// <inheritdoc />
    public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode, bool includeTracking = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = includeTracking ? dbContext.ShortUrls : dbContext.ShortUrls.AsNoTracking();

            return await query
                .FirstOrDefaultAsync(u => u.ShortCode == shortCode, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving short URL by code: {ShortCode}", shortCode);
            throw new DatabaseException("Failed to retrieve short URL by code", ex);
        }
    }


    /// <inheritdoc />
    public async Task<ShortUrl> AddAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.ShortUrls.AddAsync(shortUrl, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Created short URL with ID: {Id}, Code: {ShortCode}", shortUrl.Id, shortUrl.ShortCode);
            return shortUrl;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating short URL with code: {ShortCode}", shortUrl.ShortCode);
            throw new DatabaseException("Failed to create short URL", ex);
        }
    }


    /// <inheritdoc />
    public async Task<bool> UpdateAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.Update(shortUrl);
            var affected = await dbContext.SaveChangesAsync(cancellationToken);

            if (affected > 0)
                logger.LogInformation("Updated short URL with ID: {Id}", shortUrl.Id);

            return affected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating short URL with ID: {Id}", shortUrl.Id);
            throw new DatabaseException("Failed to update short URL", ex);
        }
    }

    
    /// <inheritdoc />
    public async Task<bool> UpdateShortCodeAsync(long id, string newShortCode, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ShortUrls
                .AsNoTracking()
                .Where(s => s.Id == id)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.ShortCode, newShortCode)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                , cancellationToken)
                .ConfigureAwait(false) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating short URL's short code with ID: {id}", id);
            throw new DatabaseException("Failed to update short URL's short code", ex);
        }
    }


    /// <inheritdoc />
    public async Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            var affected = await dbContext.ShortUrls
                .AsNoTracking()
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (affected > 0)
                logger.LogInformation("Deleted short URL with ID: {Id}", id);

            return affected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting short URL by ID: {Id}", id);
            throw new DatabaseException("Failed to delete short URL", ex);
        }
    }


    /// <inheritdoc />
    public async Task<bool> DeleteByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var affected = await dbContext.ShortUrls
                .AsNoTracking()
                .Where(s => s.ShortCode == shortCode)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (affected > 0)
                logger.LogInformation("Deleted short URL with code: {ShortCode}", shortCode);

            return affected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting short URL by code: {ShortCode}", shortCode);
            throw new DatabaseException("Failed to delete short URL", ex);
        }
    }

    
    /// <inheritdoc />
    public async Task<bool> IsShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.ShortUrls
                .AsNoTracking()
                .AnyAsync(s => s.ShortCode == shortCode, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if the URL with Short Code '{shortCode}' exists", shortCode);
            throw new DatabaseException("Failed to check if URL exists", ex);
        }
    }
}



    
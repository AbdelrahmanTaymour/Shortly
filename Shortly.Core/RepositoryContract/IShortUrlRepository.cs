using Shortly.Core.Entities;

namespace Shortly.Core.RepositoryContract;

/// <summary>
/// Contract to be implemented by ShortUrlRepository that contains data access logic of ShortUrls data store
/// </summary>
public interface IShortUrlRepository
{
    /// <summary>
    /// Retrieves all `ShortUrl` entities from the database.
    /// </summary>
    /// <returns>A list of all <see cref="ShortUrl"/> objects in the database.</returns>
    Task<List<ShortUrl>> GetAllAsync();
    
    /// <summary>
    /// Retrieves a specific `ShortUrl` entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the <see cref="ShortUrl"/>.</param>
    /// <returns>The <see cref="ShortUrl"/> instance if found, otherwise null.</returns>
    Task<ShortUrl?> GetShortUrlByIdAsync(Guid id);
    
    /// <summary>
    /// Retrieves a specific `ShortUrl` entity by its short code.
    /// </summary>
    /// <param name="shortCode">The short code associated with the <see cref="ShortUrl"/>.</param>
    /// <returns>The <see cref="ShortUrl"/> instance if found, otherwise null.</returns>
    Task<ShortUrl?> GetShortUrlByShortCodeAsync(string shortCode);
    
    /// <summary>
    /// Creates a new `ShortUrl` entity in the database.
    /// </summary>
    /// <param name="shortUrl">The <see cref="ShortUrl"/> object to be created.</param>
    /// <returns>The created <see cref="ShortUrl"/> entity.</returns>
    Task<ShortUrl?> CreateShortUrlAsync(ShortUrl shortUrl);
    
    /// <summary>
    /// Updates an existing <see cref="ShortUrl"/> entity by its unique identifier.
    /// Only the <see cref="ShortUrl.OriginalUrl"/> and <see cref="ShortUrl.ShortCode"/> properties are updated while other properties remain unchanged.
    /// </summary>
    /// <param name="id">The unique identifier of the <see cref="ShortUrl"/> to update.</param>
    /// <param name="updatedShortUrl">
    /// The updated <see cref="ShortUrl"/> object containing values for <see cref="ShortUrl.OriginalUrl"/> and <see cref="ShortUrl.ShortCode"/>.
    /// </param>
    /// <returns>The updated <see cref="ShortUrl"/> instance if found and updated, otherwise null.</returns>
    Task<ShortUrl?> UpdateShortUrlByIdAsync(Guid id, ShortUrl updatedShortUrl);
    
    /// <summary>
    /// Deletes a `ShortUrl` entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the <see cref="ShortUrl"/> to delete.</param>
    /// <returns>The deleted <see cref="ShortUrl"/> instance if found, otherwise null.</returns>
    Task<ShortUrl?> DeleteByIdAsync(Guid id);
    
    /// <summary>
    /// Deletes a `ShortUrl` entity by its short code.
    /// </summary>
    /// <param name="shortCode">The short code of the <see cref="ShortUrl"/> to delete.</param>
    /// <returns>The deleted <see cref="ShortUrl"/> instance if found, otherwise null.</returns>
    Task<ShortUrl?> DeleteByShortCodeAsync(string shortCode);
    
    /// <summary>
    /// Persists any pending database changes asynchronously.
    /// </summary>
    /// <returns>Nothing.</returns>
    Task SaveChangesAsync();

    /// <summary>
    /// Increments the access count for a `ShortUrl` entity identified by its short code.
    /// </summary>
    /// <param name="shortCode">The unique short code of the <see cref="ShortUrl"/> whose access count is to be incremented.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task IncrementAccessCountAsync(string shortCode);
    
    /// <summary>
    /// Checks if a short code already exists in the database.
    /// </summary>
    /// <param name="shortCode">The short code to check for existence.</param>
    /// <returns>True if the short code exists, otherwise false.</returns>
    Task<bool> ShortCodeExistsAsync(string shortCode);
}
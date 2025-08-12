using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UrlManagement;

/// <summary>
/// Contract to be implemented by ShortUrlRepository that contains data access logic of ShortUrls data store
/// </summary>
public interface IShortUrlRepository
{
    
    /// <summary>
    /// Retrieves a short URL entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL.</param>
    /// <param name="includeTracking">Whether to enable change tracking for the entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the ShortUrl entity if found, otherwise null.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<ShortUrl?> GetByIdAsync(long id, bool includeTracking = false, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Retrieves a short URL entity by its short code with optional tracking.
    /// </summary>
    /// <param name="shortCode">The unique short code identifier.</param>
    /// <param name="includeTracking">Whether to enable change tracking for the entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the ShortUrl entity if found, otherwise null.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Use includeTracking=true when you plan to modify the returned entity.
    /// For read-only operations, keep it false for better performance.
    /// </remarks>
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, bool includeTracking = false, CancellationToken cancellationToken = default);


    /// <summary>
    /// Creates a new short URL entity in the database.
    /// </summary>
    /// <param name="shortUrl">The short URL entity to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the created ShortUrl entity with assigned ID.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// The method validates the entity before creation and ensures short code uniqueness.
    /// </remarks>
    Task<ShortUrl> AddAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default);
 
    
    /// <summary>
    /// Updates an existing short URL entity in the database.
    /// </summary>
    /// <param name="shortUrl">The short URL entity with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the update was successful.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> UpdateAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default);
 
    
    /// <summary>
    /// Updates the short code of an existing short URL identified by its unique ID.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL to update.</param>
    /// <param name="newShortCode">The new short code to assign to the short URL.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// <c>true</c> if the short code was successfully updated; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method performs the update operation without tracking the entity in the EF Core change tracker
    /// for improved performance. It also updates the <c>UpdatedAt</c> timestamp to the current UTC time.
    /// </remarks>
    /// <exception cref="DatabaseException">
    /// Thrown when an error occurs while attempting to update the short code.
    /// </exception>
    Task<bool> UpdateShortCodeAsync(long id, string newShortCode, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Deletes a short URL entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the deletion was successful.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default);
  
    
    /// <summary>
    /// Deletes a short URL entity by its short code.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the deletion was successful.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> DeleteByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Checks whether a short URL with the specified short code exists in the database.
    /// </summary>
    /// <param name="shortCode">The short code to check for existence.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a short URL with the specified short code exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method executes the query without tracking entities in the EF Core change tracker
    /// for improved performance.
    /// </remarks>
    /// <exception cref="DatabaseException">
    /// Thrown when an error occurs while checking for the existence of the short code.
    /// </exception>
    Task<bool> IsShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);
  
}
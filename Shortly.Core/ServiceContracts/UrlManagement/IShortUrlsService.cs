using Microsoft.AspNetCore.Http;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;

namespace Shortly.Core.ServiceContracts.UrlManagement;

/// <summary>
/// Represents the contract for managing short URLs, providing functionalities for
/// creating, retrieving, updating, and deleting short URLs, as well as getting
/// statistics about the usage of a specific short URL.
/// </summary>
public interface IShortUrlsService
{
    /// <summary>
    /// Retrieves a short URL by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL</param>
    /// <param name="includeTracking">Whether to include tracking information in the result</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>A <see cref="ShortUrlDto"/> containing the short URL information</returns>
    /// <exception cref="NotFoundException">Thrown when no short URL is found with the specified ID</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    Task<ShortUrlDto> GetByIdAsync(long id, bool includeTracking = false,
        CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Retrieves a short URL by its short code.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to retrieve</param>
    /// <param name="includeTracking">Whether to include tracking information in the result</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>A <see cref="ShortUrlDto"/> containing the short URL information</returns>
    /// <exception cref="NotFoundException">Thrown when no short URL is found with the specified short code</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<ShortUrlDto> GetByShortCodeAsync(string shortCode, bool includeTracking = false,
        CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Creates a new short URL based on the provided request and HTTP context.
    /// The creation process varies depending on the owner type (anonymous, user, or organization).
    /// </summary>
    /// <param name="request">The request containing short URL creation details</param>
    /// <param name="httpContext">The HTTP context containing authentication information</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>A <see cref="CreateShortUrlResponse"/> containing the created short URL information</returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported owner type is encountered</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication requirements are not met</exception>
    /// <exception cref="ConflictException">Thrown when a custom short code already exists</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<CreateShortUrlResponse> AddAsync(CreateShortUrlRequest request, HttpContext httpContext, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Updates an existing short URL with new information.
    /// Only non-null values from the request will be applied to the existing URL.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL to update</param>
    /// <param name="shortUrl">The update request containing the new values</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>A <see cref="ShortUrlDto"/> containing the updated short URL information</returns>
    /// <exception cref="NotFoundException">Thrown when no short URL is found with the specified ID</exception>
    /// <exception cref="ServiceUnavailableException">Thrown when the update operation fails</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    Task<ShortUrlDto> UpdateByIdAsync(long id, UpdateShortUrlRequest shortUrl, CancellationToken cancellationToken = default);
  
    
    /// <summary>
    /// Updates the short code of an existing short URL.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL</param>
    /// <param name="newShortCode">The new short code to assign</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>True if the update was successful</returns>
    /// <exception cref="NotFoundException">Thrown when no short URL is found with the specified ID</exception>
    /// <exception cref="ConflictException">Thrown when a custom short code already exists</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    Task<bool> UpdateShortCodeAsync(long id, string newShortCode, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Deletes a short URL by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL to delete</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>True if the deletion was successful</returns>
    /// <exception cref="NotFoundException">Thrown when no short URL is found with the specified ID</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Deletes a short URL by its short code.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to delete</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>True if the deletion was successful</returns>
    /// <exception cref="NotFoundException">Thrown when no short URL is found with the specified short code</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    Task<bool> DeleteByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Checks whether a short code already exists in the system.
    /// </summary>
    /// <param name="shortCode">The short code to check for existence</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>True if the short code exists, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when the short code is null, empty, or whitespace</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    Task<bool> IsShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);
}
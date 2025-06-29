using Shortly.Core.DTOs;

namespace Shortly.Core.ServiceContracts;

/// <summary>
/// Represents the contract for managing short URLs, providing functionalities for
/// creating, retrieving, updating, and deleting short URLs, as well as getting
/// statistics about the usage of a specific short URL.
/// </summary>
public interface IShortUrlsService
{
    /// <summary>
    /// Retrieves a list of all short URLs stored in the system.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation, with a result containing
    /// a list of <see cref="ShortUrlResponse"/> objects representing the details of all short URLs.
    /// </returns>
    Task<List<ShortUrlResponse>> GetAllAsync();

    /// <summary>
    /// Retrieves a short URL detail based on the provided short code.
    /// </summary>
    /// <param name="shortCode">The unique short code representing the short URL.</param>
    /// <returns>
    /// A task representing the asynchronous operation, with a result containing the short URL details
    /// as a <see cref="ShortUrlResponse"/>, or null if the short code does not exist.
    /// </returns>
    Task<ShortUrlResponse?> GetByShortCodeAsync(string shortCode);

    /// <summary>
    /// Creates a new short URL based on the provided original URL request data.
    /// </summary>
    /// <param name="shortUrlRequest">An object containing the details of the original URL to be shortened.</param>
    /// <returns>
    /// A task representing the asynchronous operation, with a result containing the created short URL details
    /// as a <see cref="ShortUrlResponse"/>.
    /// </returns>
    Task<ShortUrlResponse> CreateShortUrlAsync(ShortUrlRequest shortUrlRequest);

    /// <summary>
    /// Updates the details of an existing short URL identified by the provided short code.
    /// </summary>
    /// <param name="shortCode">The unique short code representing the short URL to be updated.</param>
    /// <param name="updatedShortUrlRequest">The updated details of the short URL as a <see cref="ShortUrlRequest"/>.</param>
    /// <returns>
    /// A task representing the asynchronous operation, with a result containing the updated short URL details
    /// as a <see cref="ShortUrlResponse"/>, or null if no URL exists for the given short code.
    /// </returns>
    Task<ShortUrlResponse?> UpdateShortUrlAsync(string shortCode, ShortUrlRequest updatedShortUrlRequest);

    /// <summary>
    /// Deletes a short URL associated with the given short code.
    /// </summary>
    /// <param name="shortCode">The unique short code identifying the short URL to be deleted.</param>
    /// <returns>
    /// A task representing the asynchronous operation, with a result of true if the short URL
    /// was successfully deleted, or false if no such short URL exists.
    /// </returns>
    Task<bool> DeleteShortUrlAsync(string shortCode);

    /// <summary>
    /// Retrieves statistical information about a short URL based on the provided short code.
    /// </summary>
    /// <param name="shortCode">The unique short code associated with the short URL.</param>
    /// <returns>
    /// A task representing the asynchronous operation, with a result containing the statistics
    /// as a <see cref="StatusShortUrlResponse"/> if the short code exists, or null if it does not exist.
    /// </returns>
    Task<StatusShortUrlResponse?> GetStatisticsAsync(string shortCode);
}
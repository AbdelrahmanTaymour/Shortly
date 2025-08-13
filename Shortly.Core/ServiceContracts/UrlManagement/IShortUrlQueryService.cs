using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;

namespace Shortly.Core.ServiceContracts.UrlManagement;

/// <summary>
///     Defins query operations for retrieving short URLs based on various filters such as user, organization,
///     creation date range, expiration, and usage statistics.
/// </summary>
public interface IShortUrlQueryService
{
    /// <summary>
    ///     Retrieves a paginated list of short URLs owned by a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who owns the URLs.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based index).</param>
    /// <param name="pageSize">The maximum number of results to include per page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A collection of <see cref="ShortUrlDto" /> representing the user's short URLs.</returns>
    /// <exception cref="ValidationException">
    ///     Thrown when page number is less than 1 or page size is outside the allowed range
    ///     (1–1000).
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when a database access error occurs while retrieving data.</exception>
    Task<IEnumerable<ShortUrlDto>> GetByUserIdAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);


    /// <summary>
    ///     Retrieves a paginated list of short URLs associated with a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based index).</param>
    /// <param name="pageSize">The maximum number of results to include per page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A collection of <see cref="ShortUrlDto" /> for the specified organization.</returns>
    /// <exception cref="ValidationException">
    ///     Thrown when page number is less than 1 or page size is outside the allowed range
    ///     (1–1000).
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when a database access error occurs while retrieving data.</exception>
    Task<IEnumerable<ShortUrlDto>> GetByOrganizationIdAsync(
        Guid organizationId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);


    /// <summary>
    ///     Retrieves anonymous short URLs created within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the date range (inclusive).</param>
    /// <param name="endDate">The end date of the date range (inclusive).</param>
    /// <param name="pageNumber">The page number to retrieve (default is 1).</param>
    /// <param name="pageSize">The maximum number of results per page (default is 50).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A collection of anonymous <see cref="ShortUrlDto" /> within the specified date range.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="startDate" /> is greater than
    ///     <paramref name="endDate" />.
    /// </exception>
    /// <exception cref="ValidationException">
    ///     Thrown when page number is less than 1 or page size is outside the allowed range
    ///     (1–1000).
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when a database access error occurs while retrieving data.</exception>
    Task<IEnumerable<ShortUrlDto>> GetAnonymousUrlsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);


    /// <summary>
    ///     Retrieves short URLs that have expired by a given UTC date and time.
    /// </summary>
    /// <param name="nowUtc">The current UTC date and time to compare against the expiration date.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based index).</param>
    /// <param name="pageSize">The maximum number of results to include per page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A collection of expired <see cref="ShortUrlDto" />.</returns>
    /// <exception cref="ValidationException">
    ///     Thrown when page number is less than 1 or page size is outside the allowed range
    ///     (1–1000).
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when a database access error occurs while retrieving data.</exception>
    Task<IEnumerable<ShortUrlDto>> GetExpiredAsync(
        DateTime nowUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);


    /// <summary>
    ///     Retrieves private short URLs owned by a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who owns the URLs.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based index).</param>
    /// <param name="pageSize">The maximum number of results to include per page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A collection of private <see cref="ShortUrlDto" /> for the specified user.</returns>
    /// <exception cref="ValidationException">
    ///     Thrown when page number is less than 1 or page size is outside the allowed range
    ///     (1–1000).
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when a database access error occurs while retrieving data.</exception>
    Task<IEnumerable<ShortUrlDto>> GetPrivateLinksAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);


    /// <summary>
    ///     Retrieves short URLs created within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the date range (inclusive).</param>
    /// <param name="endDate">The end date of the date range (inclusive).</param>
    /// <param name="pageNumber">The page number to retrieve (default is 1).</param>
    /// <param name="pageSize">The maximum number of results per page (default is 50).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A collection of <see cref="ShortUrlDto" /> within the specified date range.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="startDate" /> is greater than
    ///     <paramref name="endDate" />.
    /// </exception>
    /// <exception cref="ValidationException">
    ///     Thrown when page number is less than 1 or page size is outside the allowed range
    ///     (1–1000).
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when a database access error occurs while retrieving data.</exception>
    Task<IEnumerable<ShortUrlDto>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);


    /// <summary>
    ///     Retrieves groups of duplicate short URLs based on their original URL.
    /// </summary>
    /// <param name="userId">Optional. Filter by a specific user ID.</param>
    /// <param name="organizationId">Optional. Filter by a specific organization ID.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    ///     A collection of <see cref="DuplicatesUrlsResponse" /> where each entry contains the original URL and a list of
    ///     duplicates.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when a database access error occurs while retrieving data.</exception>
    Task<IEnumerable<DuplicatesUrlsResponse>> GetDuplicateUrlsAsync(
        Guid? userId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    ///     Retrieves short URLs that have not been accessed (clicked) within a specified period.
    /// </summary>
    /// <param name="olderThan">Optional. Filters for URLs created before this time span ago.</param>
    /// <param name="pageNumber">The page number to retrieve (default is 1).</param>
    /// <param name="pageSize">The maximum number of results per page (default is 50).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A collection of unused <see cref="ShortUrlDto" />.</returns>
    /// <exception cref="ValidationException">
    ///     Thrown when page number is less than 1 or page size is outside the allowed range
    ///     (1–1000).
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when a database access error occurs while retrieving data.</exception>
    Task<IEnumerable<ShortUrlDto>> GetUnusedUrlsAsync(
        TimeSpan? olderThan = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
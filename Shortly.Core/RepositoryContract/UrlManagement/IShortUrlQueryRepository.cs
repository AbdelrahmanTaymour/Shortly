using System.Linq.Expressions;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UrlManagement;

public interface IShortUrlQueryRepository
{
    /// <summary>
    /// Searches short URLs using a custom predicate with pagination support.
    /// </summary>
    /// <param name="predicate">Expression to filter short URLs.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a read-only list of matching short URLs ordered by creation date (newest first).
    /// </returns>
    /// <exception cref="ValidationException">Thrown when pagination parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Results are ordered by CreatedAt descending. Use this method for complex
    /// filtering scenarios that aren't covered by specific methods.
    /// </remarks>
    Task<IReadOnlyList<ShortUrl>> SearchAsync(
        Expression<Func<ShortUrl, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Retrieves short URLs created by a specific user with pagination.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a read-only list of short URLs owned by the user.
    /// </returns>
    /// <exception cref="ValidationException">Thrown when pagination parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<IReadOnlyList<ShortUrl>> GetByUserIdAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Retrieves short URLs owned by a specific organization with pagination.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a read-only list of short URLs owned by the organization.
    /// </returns>
    /// <exception cref="ValidationException">Thrown when pagination parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<IReadOnlyList<ShortUrl>> GetByOrganizationIdAsync(
        Guid organizationId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Retrieves short URLs created by anonymous users within a date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// anonymous short URLs within the specified date range.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the date range is invalid.</exception>
    /// <exception cref="ValidationException">Thrown when pagination parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<IReadOnlyList<ShortUrl>> GetAnonymousUrlsByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate,
        int pageNumber = 1, 
        int pageSize = 50, 
        CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Retrieves expired short URLs with pagination for cleanup operations.
    /// </summary>
    /// <param name="nowUtc">The current UTC time to compare expiration against.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a read-only list of expired short URLs.
    /// </returns>
    /// <exception cref="ValidationException">Thrown when pagination parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<IReadOnlyList<ShortUrl>> GetExpiredAsync(
        DateTime nowUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Retrieves private short URLs for a specific user with pagination.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a read-only list of private short URLs owned by the user.
    /// </returns>
    /// <exception cref="ValidationException">Thrown when pagination parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<IReadOnlyList<ShortUrl>> GetPrivateLinksAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Searches short URLs by date range with pagination.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a read-only list of short URLs created within the specified date range.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the date range is invalid.</exception>
    /// <exception cref="ValidationException">Thrown when pagination parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<IReadOnlyList<ShortUrl>> GetByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate,
        int pageNumber = 1, 
        int pageSize = 50, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds duplicate short URLs (same original URL) for a user or organization.
    /// </summary>
    /// <param name="userId">Optional user ID to filter by.</param>
    /// <param name="organizationId">Optional organization ID to filter by.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// groups of URLs that have the same original URL.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Useful for identifying and consolidating duplicate URLs to improve management.
    /// Each group contains URLs with the same OriginalUrl.
    /// </remarks>
    Task<IReadOnlyList<IGrouping<string, ShortUrl>>> GetDuplicateUrlsAsync(
        Guid? userId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Retrieves short URLs that have never been clicked.
    /// </summary>
    /// <param name="olderThan">Optional filter for URLs older than the specified timespan.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// short URLs with zero clicks.
    /// </returns>
    /// <exception cref="ValidationException">Thrown when pagination parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Useful for identifying unused URLs that might be candidates for cleanup.
    /// </remarks>
    Task<IReadOnlyList<ShortUrl>> GetUnusedUrlsAsync(
        TimeSpan? olderThan = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
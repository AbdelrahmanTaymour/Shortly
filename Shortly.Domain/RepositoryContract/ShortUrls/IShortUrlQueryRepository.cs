using System.Linq.Expressions;
using Shortly.Domain.Entities;

namespace Shortly.Domain.RepositoryContract.ShortUrls;

/// <summary>
/// Read-only query contract for <see cref="ShortUrl"/> entities.
/// </summary>
public interface IShortUrlQueryRepository
{
    /// <summary>
    /// Returns URLs matching <paramref name="predicate"/>, paginated and
    /// ordered by <c>CreatedAt DESC</c>.
    /// </summary>
    Task<IEnumerable<ShortUrl>> SearchAsync(
        Expression<Func<ShortUrl, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
 
    /// <summary>
    /// Returns all URLs owned by <paramref name="userId"/>, paginated.
    /// Fetches the full entity — prefer <see cref="GetUserUrlsForAnalyticsAsync"/>
    /// when only analytics-relevant columns are needed.
    /// </summary>
    Task<IEnumerable<ShortUrl>> GetByUserIdAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
 
    /// <summary>
    /// Analytics-optimised read: returns only the seven columns needed by
    /// <c>UrlStatisticsService</c> (<c>Id</c>, <c>UserId</c>, <c>ShortCode</c>,
    /// <c>OriginalUrl</c>, <c>CreatedAt</c>, <c>TotalClicks</c>, <c>IsActive</c>).
    ///
    /// Do <b>not</b> pass the returned instances to code that expects a fully
    /// populated entity — fields such as <c>PasswordHash</c>, <c>Description</c>,
    /// and <c>IpAddress</c> will be their default values.
    /// </summary>
    Task<IEnumerable<ShortUrl>> GetUserUrlsForAnalyticsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
 
    Task<IEnumerable<ShortUrl>> GetByOrganizationIdAsync(
        Guid organizationId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
 
    Task<IEnumerable<ShortUrl>> GetAnonymousUrlsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
 
    Task<IEnumerable<ShortUrl>> GetExpiredAsync(
        DateTime nowUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
 
    Task<IEnumerable<ShortUrl>> GetPrivateLinksAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
 
    Task<IEnumerable<ShortUrl>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
 
    Task<IEnumerable<IGrouping<string, ShortUrl>>> GetDuplicateUrlsAsync(
        Guid? userId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);
 
    Task<IEnumerable<ShortUrl>> GetUnusedUrlsAsync(
        TimeSpan? olderThan = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for short URLs based on user-defined criteria and returns a paginated result set.
    /// </summary>
    /// <param name="userId">The unique identifier of the URL owner.</param>
    /// <param name="search">Optional term to filter by Title or OriginalUrl.</param>
    /// <param name="status">Optional status filter ("active" or "inactive").</param>
    /// <param name="visibility">Optional visibility filter ("public" or "private").</param>
    /// <param name="dateFrom">Inclusive start date for the creation range.</param>
    /// <param name="dateTo">Inclusive end date for the creation range.</param>
    /// <param name="sortBy">The sort criteria. Defaults to "newest".</param>
    /// <param name="pageNumber">The 1-based page index.</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A tuple containing the collection of <see cref="ShortUrl"/> records for the current page and the total count of all matching records.</returns>
    /// <exception cref="DatabaseException">Thrown if the query fails to execute against the data store.</exception>
    public Task<(IEnumerable<ShortUrl> items, int totalCount)> SearchByFiltersAsync(
        Guid userId,
        string? search,
        string? status,
        string? visibility,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortBy = "newest",
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
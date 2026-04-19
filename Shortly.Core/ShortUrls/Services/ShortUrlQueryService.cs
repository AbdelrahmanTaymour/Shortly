using Shortly.Core.Common;
using Shortly.Core.ShortUrls.Contracts;
using Shortly.Core.ShortUrls.DTOs;
using Shortly.Core.ShortUrls.Mappers;
using Shortly.Domain.RepositoryContract.ShortUrls;

namespace Shortly.Core.ShortUrls.Services;

/// <summary>
///     Provides query operations for retrieving short URLs based on various filters such as user, organization,
///     creation date range, expiration, and usage statistics.
///     Wraps repository results and maps them to DTOs for API consumption.
/// </summary>
public class ShortUrlQueryService(IShortUrlQueryRepository queryRepository) : IShortUrlQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrlDto>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var urls = await queryRepository.GetByUserIdAsync(userId, pageNumber, pageSize, cancellationToken);
        return urls.MapToShortUrlDtos();
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrlDto>> GetByOrganizationIdAsync(Guid organizationId, int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var urls = await queryRepository.GetByOrganizationIdAsync(organizationId, pageNumber, pageSize,
            cancellationToken);
        return urls.MapToShortUrlDtos();
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrlDto>> GetAnonymousUrlsByDateRangeAsync(DateTime startDate, DateTime endDate,
        int pageNumber = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var urls = await queryRepository.GetAnonymousUrlsByDateRangeAsync(startDate, endDate, pageNumber, pageSize,
            cancellationToken);
        return urls.MapToShortUrlDtos();
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrlDto>> GetExpiredAsync(DateTime nowUtc, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var urls = await queryRepository.GetExpiredAsync(nowUtc, pageNumber, pageSize, cancellationToken);
        return urls.MapToShortUrlDtos();
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrlDto>> GetPrivateLinksAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var urls = await queryRepository.GetPrivateLinksAsync(userId, pageNumber, pageSize, cancellationToken);
        return urls.MapToShortUrlDtos();
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrlDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate,
        int pageNumber = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var urls = await queryRepository.GetByDateRangeAsync(startDate, endDate, pageNumber, pageSize,
            cancellationToken);
        return urls.MapToShortUrlDtos();
    }


    /// <inheritdoc />
    public async Task<IEnumerable<DuplicatesUrlsResponse>> GetDuplicateUrlsAsync(Guid? userId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var groupedEntities = await queryRepository.GetDuplicateUrlsAsync(userId, organizationId, cancellationToken);

        return groupedEntities
            .SelectMany(group => group.MapToShortUrlDtos())
            .GroupBy(dto => dto.OriginalUrl)
            .Select(g => new DuplicatesUrlsResponse
            (
                g.Key,
                g.ToList()
            ));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ShortUrlDto>> GetUnusedUrlsAsync(TimeSpan? olderThan = null, int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var urls = await queryRepository.GetUnusedUrlsAsync(olderThan, pageNumber, pageSize, cancellationToken);
        return urls.MapToShortUrlDtos();
    }

    public async Task<PagedResult<ShortUrlDto>> SearchByFiltersAsync(
        Guid userId,
        ShortUrlSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var (shortUrls, totalCount) =
            await queryRepository.SearchByFiltersAsync(userId, request.Search, request.Status, request.Visibility,
                request.DateFrom, request.DateTo, request.SortBy, request.PageNumber, request.PageSize,
                cancellationToken);

        var dtoItems = shortUrls.MapToShortUrlDtos();

        return new PagedResult<ShortUrlDto>(
            dtoItems,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
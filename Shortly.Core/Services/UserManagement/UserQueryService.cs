using System.Linq.Expressions;
using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.UserManagement;

/// <summary>
/// Provides implementation for querying users using various filters and criteria.
/// </summary>
public class UserQueryService(IUserRepository userRepository, IUserSecurityRepository securityRepository)
    : IUserQueryService
{
    /// <inheritdoc />
     public async Task<BasicUserSearchResponse> SearchBasicUsersAsync(
        UserSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await userRepository
            .SearchUsers(request, false, cancellationToken);

        var basicUsers = users.Cast<UserSearchResult>();
        var totalPages = CalculateTotalPages(totalCount, request.PageSize);

        return new BasicUserSearchResponse(basicUsers, totalCount, request.Page, request.PageSize, totalPages);
    }

    /// <inheritdoc />
     public async Task<CompleteUserSearchResponse> SearchCompleteUsersAsync(
        UserSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await userRepository
            .SearchUsers(request, true, cancellationToken);

        var completeUsers = users.Cast<CompleteUserSearchResult>();
        var totalPages = CalculateTotalPages(totalCount, request.PageSize);

        return new CompleteUserSearchResponse(completeUsers, totalCount, request.Page, request.PageSize, totalPages);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetUsersBySubscriptionPlanAsync(enSubscriptionPlan plan, int page,
        int pageSize, CancellationToken cancellationToken = default)
    {
        return await GetUsersByCustomCriteriaAsync(u => u.SubscriptionPlanId == plan,
            page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetActiveUsersAsync(int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await GetUsersByCustomCriteriaAsync(u => u.IsActive == true,
            page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetInactiveUsersAsync(int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await GetUsersByCustomCriteriaAsync(u => u.IsActive == false,
            page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetUnverifiedUsersAsync(int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await GetUsersByCustomCriteriaAsync(u => u.IsEmailConfirmed == false,
            page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetLockedUsersAsync(int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await securityRepository.GetLockedUsersAsync(page, pageSize, cancellationToken);
        return result.MapToUserProfileDtoList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetUsersByCustomCriteriaAsync(Expression<Func<User, bool>> predicateint,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetUsersByCustomCriteriaAsync(predicateint, page, pageSize, cancellationToken);
        return users.MapToUserProfileDtoList();
    }


    #region Private Helper Methods

    /// <summary>
    /// Calculates the total number of pages based on total count and page size.
    /// </summary>
    /// <param name="totalCount">Total number of items.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Total number of pages.</returns>
    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        return (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    #endregion
}
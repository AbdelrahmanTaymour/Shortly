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
    public async Task<UserSearchResponse> AdvancedSearchUsersAsync(string? searchTerm,
        enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        bool? isDeleted,
        bool? isEmailConfirmed,
        int page,
        int pageSize,
        bool retrieveCompleteUser = false,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;


        var (users, totalCount) = await userRepository
            .SearchUsers(searchTerm, subscriptionPlan, isActive, isDeleted, isEmailConfirmed, page, pageSize,
                retrieveCompleteUser, cancellationToken);


        return new UserSearchResponse
        (
            users,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );
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
}
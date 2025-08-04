using System.Linq.Expressions;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.DTOs.UsersDTOs.Profile;
using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts.UserManagement;

/// <summary>
/// Provides query operations for retrieving user data based on various filters and conditions.
/// </summary>
public interface IUserQueryService
{
    /// <summary>
    /// Performs an advanced search for users based on optional filters.
    /// </summary>
    /// <param name="searchTerm">An optional term to match against usernames, emails, or other searchable fields.</param>
    /// <param name="subscriptionPlan">Optional subscription plan to filter users by.</param>
    /// <param name="isActive">Filter by user activity status.</param>
    /// <param name="isDeleted">Filter by whether the user is deleted.</param>
    /// <param name="isEmailConfirmed">Filter by email confirmation status.</param>
    /// <param name="page">Page number to retrieve (1-based index).</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="retrieveCompleteUser">If true, retrieves extended user information (e.g., with related entities).</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of users matching the search criteria and metadata.</returns>
    Task<UserSearchResponse> AdvancedSearchUsersAsync(string? searchTerm,
        enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        bool? isDeleted,
        bool? isEmailConfirmed,
        int page,
        int pageSize,
        bool retrieveCompleteUser = false,
        CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves users assigned to a specific subscription plan.
    /// </summary>
    /// <param name="plan">The subscription plan to filter users by.</param>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of users under the specified subscription plan.</returns>
    Task<IEnumerable<UserDto>> GetUsersBySubscriptionPlanAsync(enSubscriptionPlan plan, int page, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves users who are marked as active.
    /// </summary>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of active users.</returns>
    Task<IEnumerable<UserDto>> GetActiveUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves users who are marked as inactive.
    /// </summary>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of inactive users.</returns>
    Task<IEnumerable<UserDto>> GetInactiveUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves users whose email addresses have not been confirmed.
    /// </summary>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of unverified users.</returns>
    Task<IEnumerable<UserDto>> GetUnverifiedUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves users who are currently locked (e.g., due to security policies).
    /// </summary>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of locked users.</returns>
    Task<IEnumerable<UserDto>> GetLockedUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Retrieves users based on a custom predicate.
    /// </summary>
    /// <param name="predicateint">An expression used to filter users.</param>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of users matching the specified criteria.</returns>
    Task<IEnumerable<UserDto>> GetUsersByCustomCriteriaAsync(Expression<Func<User, bool>> predicateint, int page
        , int pageSize, CancellationToken cancellationToken = default);
}
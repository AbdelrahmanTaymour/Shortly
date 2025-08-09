using System.Linq.Expressions;
using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts.UserManagement;

/// <summary>
/// Provides query operations for retrieving user data based on various filters and conditions.
/// </summary>
public interface IUserQueryService
{

    /// <summary>
    /// Performs a search for users and returns basic user information only.
    /// </summary>
    /// <param name="request">The search request containing filter criteria and pagination parameters.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A response containing basic user information with pagination metadata.</returns>
    /// <exception cref="ValidationException">Thrown when page or pageSize are outside valid bounds.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<BasicUserSearchResponse> SearchBasicUsersAsync(UserSearchRequest request, CancellationToken cancellationToken = default);


    /// <summary>
    /// Performs a search for users and returns complete user information with all related data.
    /// </summary>
    /// <param name="request">The search request containing filter criteria and pagination parameters.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A response containing complete user information with pagination metadata.</returns>
    /// <exception cref="ValidationException">Thrown when page or pageSize are outside valid bounds.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<CompleteUserSearchResponse> SearchCompleteUsersAsync(UserSearchRequest request, CancellationToken cancellationToken = default);


    /// <summary>
    /// Retrieves users assigned to a specific subscription plan.
    /// </summary>
    /// <param name="plan">The subscription plans to filter users by.</param>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of users under the specified subscription plan.</returns>
    Task<IEnumerable<UserDto>> GetUsersBySubscriptionPlanAsync(enSubscriptionPlan plan, int page, int pageSize,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Retrieves users who are marked as active.
    /// </summary>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of active users.</returns>
    Task<IEnumerable<UserDto>> GetActiveUsersAsync(int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves users who are marked as inactive.
    /// </summary>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of inactive users.</returns>
    Task<IEnumerable<UserDto>> GetInactiveUsersAsync(int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves users whose email addresses have not been confirmed.
    /// </summary>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of unverified users.</returns>
    Task<IEnumerable<UserDto>> GetUnverifiedUsersAsync(int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves users who are currently locked (e.g., due to security policies).
    /// </summary>
    /// <param name="page">Page number to retrieve.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A paginated list of locked users.</returns>
    Task<IEnumerable<UserDto>> GetLockedUsersAsync(int page, int pageSize,
        CancellationToken cancellationToken = default);


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
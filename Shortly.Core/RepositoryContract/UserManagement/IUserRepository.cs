using System.Linq.Expressions;
using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.RepositoryContract.UserManagement;

/// <summary>
/// Repository interface for managing user data operations.
/// Provides comprehensive methods for user CRUD operations, existence checks, and queries.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<User?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Retrieves a user by their email address, excluding deleted users.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user if found and not deleted; otherwise, null.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a user by their username, excluding deleted users.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user if found and not deleted; otherwise, null.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a user by either their email address or username, excluding deleted users.
    /// </summary>
    /// <param name="emailOrUsername">The email or username to search for.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user if found and not deleted; otherwise, null.</returns>
    Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a user with their associated profile information, excluding deleted users.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user with profile if found and not deleted; otherwise, null.</returns>
    Task<User?> GetWithProfileAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a user with their associated security information, excluding deleted users.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user with security information if found and not deleted; otherwise, null.</returns>
    Task<User?> GetWithSecurityAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user with their associated usage information, excluding deleted users.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user with usage information if found and not deleted; otherwise, null.</returns>
    Task<User?> GetWithUsageAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a complete user with all associated entities (profile, security, usage, subscription), excluding deleted users.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The complete user with all related data if found and not deleted; otherwise, null.</returns>
    Task<User?> GetCompleteUserAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new user along with their associated profile, security, and usage records in a single transaction.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The created user entity.</returns>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing user in the database.
    /// </summary>
    /// <param name="user">The user entity with updated information.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs a soft delete on a user by marking them as deleted with audit information.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="deletedBy">The unique identifier of the user performing the deletion.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user exists and is not deleted.
    /// </summary>
    /// <param name="id">The unique identifier of the user to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the user exists and is not deleted; otherwise, false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an email address is already in use by a non-deleted user.
    /// </summary>
    /// <param name="email">The email address to check for existence.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the email exists and belongs to a non-deleted user; otherwise, false.</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an username address is already in use by a non-deleted user.
    /// </summary>
    /// <param name="username">The username address to check for existence.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the username exists and belongs to a non-deleted user; otherwise, false.</returns>
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if either an email address or username is already in use by a non-deleted user.
    /// </summary>
    /// <param name="email">The email address to check for existence.</param>
    /// <param name="username">The username to check for existence.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if either the email or username exists and belongs to a non-deleted user; otherwise, false.</returns>
    Task<bool> EmailOrUsernameExistsAsync(string email, string username, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a paginated list of non-deleted users ordered by creation date.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of users per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A collection of users for the specified page.</returns>
    Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of users that match the given custom criteria.
    /// </summary>
    /// <param name="predicateint">An expression used to filter users based on custom logic.</param>
    /// <param name="page">The page number to retrieve (1-based index). Defaults to 1.</param>
    /// <param name="pageSize">The number of users to retrieve per page. Defaults to 10.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an <see cref="IEnumerable{User}"/>
    /// of users that match the specified criteria and fall within the specified page.
    /// </returns>
    Task<IEnumerable<User>> GetUsersByCustomCriteriaAsync(Expression<Func<User, bool>> predicateint, int page = 1,
        int pageSize = 10, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Searches for users based on optional filtering criteria with support for pagination.
    /// Allows returning either basic or complete user information.
    /// </summary>
    /// <param name="searchTerm">
    ///     Optional term to search by email or username. Case-insensitive partial matches are supported.
    /// </param>
    /// <param name="subscriptionPlan">
    ///     Optional subscription plan filter. If specified, limits results to users with the given plan.
    /// </param>
    /// <param name="isActive">
    ///     Optional filter to include only active or inactive users.
    /// </param>
    /// <param name="isDeleted">
    ///     Optional filter to include only deleted or non-deleted users.
    /// </param>
    /// <param name="isEmailConfirmed">
    ///     Optional filter to include only confirmed or not-confirmed emails.
    /// </param>
    /// <param name="page">
    ///     The 1-based page number. Must be greater than 0.
    /// </param>
    /// <param name="pageSize">
    ///     The number of users to return per page. Must be between 1 and 1000.
    /// </param>
    /// <param name="retrieveCompleteUser">
    ///     If true, includes detailed related data (profile, security, usage) per user. 
    ///     If false, returns only basic user information.
    /// </param>
    ///  /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     A tuple containing:
    ///     <list type="bullet">
    ///         <item><description>A collection of <see cref="IUserSearchResult"/> records (either <see cref="UserSearchResult"/> or <see cref="CompleteUserSearchResult"/>)</description></item>
    ///         <item><description>The total number of matching users</description></item>
    ///     </list>
    /// </returns>
    Task<(IEnumerable<IUserSearchResult> Users, int TotalCount)> SearchUsers(
        string? searchTerm = null,
        enSubscriptionPlan? subscriptionPlan = null,
        bool? isActive = null,
        bool? isDeleted = null,
        bool? isEmailConfirmed = null,
        int page = 1,
        int pageSize = 10,
        bool retrieveCompleteUser = false,
        CancellationToken cancellationToken = default);
}
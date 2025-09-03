using System.Linq.Expressions;
using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
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
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their email address, excluding deleted users.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user if found and not deleted; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their username, excluding deleted users.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user if found and not deleted; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by either their email address or username, excluding deleted users.
    /// </summary>
    /// <param name="emailOrUsername">The email or username to search for.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user if found and not deleted; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user with their associated profile information, excluding deleted users.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user with profile if found and not deleted; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses eager loading with Include to fetch profile data in a single query.</remarks>
    Task<User?> GetWithProfileAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user with their associated security information, excluding deleted users.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user with security information if found and not deleted; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses eager loading with Include to fetch security data in a single query.</remarks>
    Task<User?> GetWithSecurityAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user with their associated usage information, excluding deleted users.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user with usage information if found and not deleted; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses eager loading with Include to fetch usage data in a single query.</remarks>
    Task<User?> GetWithUsageAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a complete user with all associated entities (profile, security, usage, subscription), excluding deleted users.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The complete user with all related data if found and not deleted; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Loads all related entities in a single query - use carefully as this can be expensive for large datasets.</remarks>
    Task<User?> GetCompleteUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the <see cref="enSubscriptionPlan"/> identifier associated with the specified user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the user's <see cref="enSubscriptionPlan"/> ID.
    /// </returns>
    /// <exception cref="DatabaseException">
    /// Thrown when the subscription plan could not be retrieved due to a database error.
    /// </exception>
    /// <remarks>
    /// This method performs a read-only query using <c>AsNoTracking()</c> and filters out soft-deleted users. 
    /// It logs any unexpected exceptions and wraps them in a <see cref="DatabaseException"/> for higher-level handling.
    /// </remarks>
    Task<enSubscriptionPlan> GetSubscriptionPlanIdAsync(Guid id, CancellationToken cancellationToken = default);


    /// <summary>
    /// Creates a new user along with their associated profile, security, and usage records in a single transaction.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <returns>The created user entity.</returns>
    /// <exception cref="ValidationException">Thrown when user ID is empty.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    ///     Creates user and all related entities (UserProfile, UserSecurity, UserUsage) in a single transaction.
    ///     Uses bulk insert with AddRangeAsync for optimal performance.
    /// </remarks>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Updates an existing user in the database.
    /// </summary>
    /// <param name="user">The user entity with updated information.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> UpdateAsync(User user);
    
    /// <summary>
    /// Changes the email address of a user and marks it as confirmed.
    /// </summary>
    /// <param name="id">The unique identifier of the user whose email is to be changed.</param>
    /// <param name="newEmail">The new email address to assign to the user.</param>
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the email was successfully updated; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when there is a failure to update the email in the database.</exception>
    Task<bool> ChangeEmailAsync(Guid id, string newEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the password of a user identified by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user whose password is being changed.</param>
    /// <param name="newPasswordHash">The new password hash to set for the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the password was successfully changed; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> ChangePasswordAsync(Guid id, string newPasswordHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a soft delete on a user by marking them as deleted with audit information.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="deletedBy">The unique identifier of the user performing the deletion.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses ExecuteUpdateAsync for high-performance bulk update without loading entity into memory.</remarks>
    Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a user by setting <c>IsActive</c> to true and updating the <c>UpdatedAt</c> timestamp.
    /// </summary>
    /// <param name="id">The unique identifier of the user to activate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is <c>true</c> if the user was successfully activated; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="DatabaseException">
    /// Thrown if an error occurs during the activation process in the database.
    /// </exception>
    Task<bool> ActivateUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user by setting <c>IsActive</c> to false and updating the <c>UpdatedAt</c> timestamp.
    /// </summary>
    /// <param name="id">The unique identifier of the user to deactivate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is <c>true</c> if the user was successfully deactivated; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="DatabaseException">
    /// Thrown if an error occurs during the deactivation process in the database.
    /// </exception>
    Task<bool> DeactivateUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the user's email as confirmed in the system.
    /// </summary>
    /// <param name="id">The unique identifier of the user whose email is being confirmed.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database operation fails.</exception>
    Task<bool> MarkEmailAsConfirmedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user exists and is not deleted.
    /// </summary>
    /// <param name="id">The unique identifier of the user to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the user exists and is not deleted; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email address is already in use by a non-deleted user.
    /// </summary>
    /// <param name="email">The email address to check for existence.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the email exists or belongs to a deleted user; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an username address is already in use by a non-deleted user.
    /// </summary>
    /// <param name="username">The username address to check for existence.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the username exists or belongs to a deleted user; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if either an email address or username is already in use by a non-deleted user.
    /// </summary>
    /// <param name="email">The email address to check for existence.</param>
    /// <param name="username">The username to check for existence.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if either the email or username exists or belongs to a deleted user; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> EmailOrUsernameExistsAsync(string email, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of non-deleted users ordered by creation date.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of users per page (1-1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A collection of users for the specified page.</returns>
    /// <exception cref="ValidationException">Thrown when page or pageSize parameters are invalid.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Results are ordered by <c>CreatedAt</c> in ascending order.
    /// </remarks>
    Task<IEnumerable<User>> GetUsersByCustomCriteriaAsync(Expression<Func<User, bool>> predicateint, int page = 1,
        int pageSize = 10, CancellationToken cancellationToken = default);


    /// <summary>
    /// Performs a paginated search for users based on the specified criteria.
    /// </summary>
    /// <param name="request">The search request containing filter criteria and pagination parameters.</param>
    /// <param name="retrieveCompleteUser">If true, includes related entities (Profile, UserSecurity, UserUsage); otherwise returns basic user information only.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    ///     A tuple containing:
    ///     <list type="bullet">
    ///         <item><description>A collection of <see cref="IUserSearchResult"/> records (either <see cref="UserSearchResult"/> or <see cref="CompleteUserSearchResult"/>)</description></item>
    ///         <item><description>The total number of matching users</description></item>
    ///     </list>
    /// </returns>
    /// <exception cref="ValidationException">
    ///     Thrown when <paramref name="request" /> is invalid
    /// </exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<(IEnumerable<IUserSearchResult> Users, int TotalCount)> SearchUsers(UserSearchRequest request,
        bool retrieveCompleteUser = false, CancellationToken cancellationToken = default);
}
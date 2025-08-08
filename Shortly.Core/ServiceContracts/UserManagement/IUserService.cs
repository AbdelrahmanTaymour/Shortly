using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;

namespace Shortly.Core.ServiceContracts.UserManagement;

/// <summary>
/// Defines core business logic for managing user accounts, including creation, updates,
/// activation/deactivation, availability checks, and soft deletion.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique ID of the user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user's details as a <see cref="UserDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    Task<UserDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user's details as a <see cref="UserDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    Task<UserDto> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their username.
    /// </summary>
    /// <param name="username">The username of the user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user's details as a <see cref="UserDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    Task<UserDto> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user based on the provided request.
    /// </summary>
    /// <param name="request">The user creation request containing all required data.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created user's details as a <see cref="CreateUserResponse"/>.</returns>
    /// <exception cref="ConflictException">Thrown if the email or username is already taken.</exception>
    /// <exception cref="DatabaseException">Thrown if the user could not be created due to internal error.</exception>
    Task<CreateUserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user's details.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="dto">The updated user data.</param>
    /// <returns>The updated user's details as a <see cref="UserDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="ConflictException">Thrown if the new username is already taken.</exception>
    /// <exception cref="ServiceUnavailableException">Thrown if the update operation fails.</exception>
    Task<UserDto> UpdateAsync(Guid userId, UpdateUserDto dto);


    /// <summary>
    /// Performs a soft delete on a user by marking them as deleted.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <param name="deletedBy">The ID of the user performing the deletion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the user was successfully soft deleted; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    Task<bool> SoftDeleteAsync(Guid userId, Guid deletedBy, CancellationToken cancellationToken = default);


    /// <summary>
    /// Activates a user account by setting the <c>IsActive</c> flag to true.
    /// </summary>
    /// <param name="userId">The ID of the user to activate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the user was successfully activated; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    Task<bool> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user account by setting the <c>IsActive</c> flag to false.
    /// </summary>
    /// <param name="userId">The ID of the user to deactivate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the user was successfully deactivated; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    Task<bool> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);


    /// <summary>
    /// Checks whether a user with the given ID exists.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the user exists; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a username is available (i.e., not already taken).
    /// </summary>
    /// <param name="username">The username to check for availability.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the username is available; otherwise, <c>false</c>.</returns>
    Task<bool> IsUsernameAvailableAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an email is available (i.e., not already taken).
    /// </summary>
    /// <param name="email">The email to check for availability.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the email is available; otherwise, <c>false</c>.</returns>
    Task<bool> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default);
}
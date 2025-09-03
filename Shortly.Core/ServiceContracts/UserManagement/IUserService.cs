using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

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
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<UserDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user's details as a <see cref="UserDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<UserDto> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their username.
    /// </summary>
    /// <param name="username">The username of the user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user's details as a <see cref="UserDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<UserDto> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their email or username.
    /// </summary>
    /// <param name="emailOrUsername">The email or username of the user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user's details as a <see cref="User"/>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<User> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new user based on the provided request.
    /// </summary>
    /// <param name="request">The user creation request containing all required data.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created user's details as a <see cref="CreateUserResponse"/>.</returns>
    /// <exception cref="ConflictException">Thrown if the email or username is already taken.</exception>
    /// <exception cref="DatabaseException">Thrown if the user could not be created due to an internal error.</exception>
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user's details.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="request">The updated user data.</param>
    /// <returns>The updated user's details as a <see cref="UserDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="ConflictException">Thrown if the new username is already taken.</exception>
    /// <exception cref="ServiceUnavailableException">Thrown if the update operation fails.</exception>
    Task<UserDto> UpdateAsync(Guid userId, UpdateUserRequest request);
    
    /// <summary>
    /// Updates the email address of a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose email address will be changed.</param>
    /// <param name="newEmail">The new email address to assign to the user.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Returns a boolean indicating whether the email address was successfully changed.</returns>
    /// <exception cref="DatabaseException">Thrown when there is a failure to update the email in the database.</exception>
    Task<bool> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the password for a specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose password is to be changed.</param>
    /// <param name="newPassword">The new password to set for the user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the password was successfully updated; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when the password update operation fails in the database.</exception>
    Task<bool> ChangePasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a soft delete on a user by marking them as deleted.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <param name="deletedBy">The ID of the user performing the deletion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the user was successfully softly deleted; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> SoftDeleteAsync(Guid userId, Guid deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the email associated with a specific user as confirmed.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose email will be marked as confirmed.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the email confirmation was successful.</returns>
    /// <exception cref="DatabaseException">Thrown when a database operation fails.</exception>
     Task<bool> MarkEmailAsConfirmedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether the provided current password matches the stored password for the specified user.
    /// </summary>
    /// <param name="userId">The unique ID of the user.</param>
    /// <param name="currentPassword">The current password to be validated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user's details as a <see cref="UserDto"/> if the password is valid; otherwise, null.</returns>
    /// <exception cref="UnauthorizedException">Thrown if the provided password is incorrect.</exception>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="DatabaseException">Thrown when a database operation error occurs.</exception>
    Task<UserDto?> ValidateCurrentPasswordAsync(Guid userId, string currentPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a user account by setting the <c>IsActive</c> flag to true.
    /// </summary>
    /// <param name="userId">The ID of the user to activate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the user was successfully activated; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user account by setting the <c>IsActive</c> flag to false.
    /// </summary>
    /// <param name="userId">The ID of the user to deactivate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the user was successfully deactivated; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

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
    /// Checks whether a user with the given ID exists.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the user exists; otherwise, <c>false</c>.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a username is available (i.e., not already taken).
    /// </summary>
    /// <param name="username">The username to check for availability.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the username is available; otherwise, <c>false</c>.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> IsUsernameAvailableAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an email is available (i.e., not already taken).
    /// </summary>
    /// <param name="email">The email to check for availability.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the email is available and ready to use; otherwise, <c>false</c>.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default);
}
using System.ComponentModel.DataAnnotations;
using MethodTimer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

/// <summary>
/// Provides RESTful API endpoints for user management operations including CRUD operations,
/// user activation/deactivation, and availability checks.
/// </summary>
/// <remarks>
/// This controller handles all user-related operations with proper error handling,
/// authentication, and authorization through permission-based access control.
/// </remarks>
[ApiController]
[Route("api/user")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class UserController(IUserService userService) : ControllerApiBase
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="UserDto"/> containing the user's information if found.
    /// </returns>
    /// <response code="200">Returns the user information successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view users.</response>
    /// <response code="404">User with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    /// GET /api/user/by-Id/12345678-1234-1234-1234-123456789012
    /// </example>
    [HttpGet("by-Id/{userId:guid:required}", Name = "GetUserById")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewUsers)]
    [Time]
    public async Task<IActionResult> GetUserById(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userService.GetByIdAsync(userId, cancellationToken);
        return Ok(user);
    }
    
    
    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address of the user to retrieve. Must be a valid email format.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="UserDto"/> containing the user's information if found.
    /// </returns>
    /// <response code="200">Returns the user information successfully.</response>
    /// <response code="400">The provided email address is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view users.</response>
    /// <response code="404">User with the specified email was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    /// GET /api/user/by-email/?email=user@example.com
    /// </example>
    [HttpGet("by-email/", Name = "GetUserByEmail")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewUsers)]
    [Time]
    public async Task<IActionResult> GetUserByEmail([FromQuery] [EmailAddress] string email, CancellationToken cancellationToken = default)
    {
        var user = await userService.GetByEmailAsync(email, cancellationToken);
        return Ok(user);
    }
    
    
    /// <summary>
    /// Retrieves a user by their username.
    /// </summary>
    /// <param name="username">The username of the user to retrieve.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="UserDto"/> containing the user's information if found.
    /// </returns>
    /// <response code="200">Returns the user information successfully.</response>
    /// <response code="400">The provided username is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view users.</response>
    /// <response code="404">User with the specified username was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    /// GET /api/user/by-username/?username=johnsmith
    /// </example>
    [HttpGet("by-username/", Name = "GetUserByUsername")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewUsers)]
    [Time]
    public async Task<IActionResult> GetUserByUsername([FromQuery] string username, CancellationToken cancellationToken = default)
    {
        var user = await userService.GetByUsernameAsync(username, cancellationToken);
        return Ok(user);
    }

  
    /// <summary>
    /// Creates a new user in the system.
    /// </summary>
    /// <param name="request">The user creation request containing all required user information.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="CreateUserResponse"/> containing the created user's information and ID.
    /// </returns>
    /// <response code="200">User created successfully. Returns the created user information.</response>
    /// <response code="400">The request data is invalid or missing required fields.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to add users.</response>
    /// <response code="409">A user with the same email or username already exists.</response>
    /// <response code="500">An internal server error occurred during user creation.</response>
    /// <example>
    /// POST /api/user
    /// Body: { "email": "user@example.com", "username": "johnsmith", "firstName": "John", "lastName": "Smith" }
    /// </example>
    [HttpPost(Name = "CreateNewUser")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.CreateUser)]
    public async Task<IActionResult> CreateNewUser([FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction("GetUserById", new { userId = user.Id }, user);
    }


    /// <summary>
    /// Updates an existing user's information.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="request">The user update request containing the modified user information.</param>
    /// <returns>
    /// A <see cref="UserDto"/> containing the updated user's information.
    /// </returns>
    /// <response code="200">User updated successfully. Returns the updated user information.</response>
    /// <response code="400">The request data is invalid or the user ID is malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update users.</response>
    /// <response code="409">The update would create a conflict (e.g., duplicate email/username).</response>
    /// <response code="500">An internal server error occurred during the update.</response>
    /// <example>
    /// PUT /api/user/12345678-1234-1234-1234-123456789012
    /// Body: { "firstName": "John", "lastName": "Doe", "email": "john.doe@example.com" }
    /// </example>
    [HttpPut("{userId:guid:required}", Name = "UpdateUser")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.UpdateUser)]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
    {
        var updatedUser = await userService.UpdateAsync(userId, request);
        return Ok(updatedUser);
    }

    
    /// <summary>
    /// Performs a soft delete on a user, marking them as deleted without permanently removing the record.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// No content on successful deletion.
    /// </returns>
    /// <response code="204">User deleted successfully. No content returned.</response>
    /// <response code="400">The provided user ID is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to delete users.</response>
    /// <response code="404">User with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred during deletion.</response>
    /// <remarks>
    /// This operation performs a soft delete, meaning the user record is marked as deleted
    /// but remains in the database for audit purposes. The current user's ID is automatically
    /// recorded as the one who performed the deletion.
    /// </remarks>
    /// <example>
    /// DELETE /api/user/delete/12345678-1234-1234-1234-123456789012
    /// </example>
    [HttpDelete("delete/{userId:guid:required}", Name = "DeleteUser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.DeleteUser)]   
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken = default)
    {
        var deletedBy = GetCurrentUserId();
        await userService.SoftDeleteAsync(userId, deletedBy, cancellationToken);
        return NoContent();
    }

    
    /// <summary>
    /// Activates a user account, enabling them to access the system.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to activate.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A success message indicating the user was activated.
    /// </returns>
    /// <response code="200">User activated successfully.</response>
    /// <response code="400">The provided user ID is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to control user activation.</response>
    /// <response code="404">User with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred during activation.</response>
    /// <example>
    /// PUT /activate/12345678-1234-1234-1234-123456789012
    /// </example>
    [HttpPut("/activate/{userId:guid:required}", Name = "ActivateUser")]
    [ProducesResponseType(typeof(bool),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReactivateUser)]
    public async Task<IActionResult> ActivateUser(Guid userId, CancellationToken cancellationToken = default)
    {
        await userService.ActivateUserAsync(userId, cancellationToken);
        return Ok("User activated successfully");
    }
    
    
    /// <summary>
    /// Deactivates a user account, preventing them from accessing the system.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to deactivate.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A success message indicating the user was deactivated.
    /// </returns>
    /// <response code="200">User deactivated successfully.</response>
    /// <response code="400">The provided user ID is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to control user activation.</response>
    /// <response code="404">User with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred during deactivation.</response>
    /// <example>
    /// PUT /deactivate/12345678-1234-1234-1234-123456789012
    /// </example>
    [HttpPut("/deactivate/{userId:guid:required}", Name = "DeactivateUser")]
    [ProducesResponseType(typeof(bool),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.DeactivateUser)]
    public async Task<IActionResult> DeactivateUser(Guid userId, CancellationToken cancellationToken = default)
    {
        await userService.DeactivateUserAsync(userId, cancellationToken);
        return Ok("User deactivated successfully");
    }

    
    /// <summary>
    /// Checks whether a user exists in the system by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to check.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A boolean value indicating whether the user exists (true) or not (false).
    /// </returns>
    /// <response code="200">Returns true if the user exists, false otherwise.</response>
    /// <response code="400">The provided user ID is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to check user availability.</response>
    /// <response code="500">An internal server error occurred during the check.</response>
    /// <example>
    /// GET /api/user/exists/12345678-1234-1234-1234-123456789012
    /// </example>
    [HttpGet("exists/{userId:guid:required}", Name = "IsUserExists")]
    [ProducesResponseType(typeof(bool),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ManageUserAccessibility)]
    public async Task<IActionResult> IsUserExists(Guid userId, CancellationToken cancellationToken = default)
    {
        var isExists = await userService.ExistsAsync(userId, cancellationToken);
        return Ok(isExists);
    }
    
    
    
    /// <summary>
    /// Checks whether an email address is available for registration (not already in use).
    /// </summary>
    /// <param name="email">The email address to check for availability. Must be a valid email format.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A boolean value indicating whether the email is available for use (true) or already taken (false).
    /// </returns>
    /// <response code="200">Returns true if the email is available, false if already in use.</response>
    /// <response code="400">The provided email address is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to check email availability.</response>
    /// <response code="500">An internal server error occurred during the check.</response>
    /// <remarks>
    /// This endpoint allows anonymous access to facilitate user registration processes.
    /// Returns true when the email is ready to use (not taken by another user).
    /// </remarks>
    /// <example>
    /// GET /api/user/email-availability/?email=newuser@example.com
    /// </example>
    [HttpGet("email-availability/", Name = "IsEmailAvailable")]
    [ProducesResponseType(typeof(bool),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> IsEmailAvailable([FromQuery] [EmailAddress] string email, CancellationToken cancellationToken = default)
    {
        var isAvailable = await userService.IsEmailAvailableAsync(email, cancellationToken);
        return Ok(isAvailable);
    }
    
    /// <summary>
    /// Checks whether a username is available for registration (not already in use).
    /// </summary>
    /// <param name="username">The username to check for availability.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A boolean value indicating whether the username is available for use (true) or already taken (false).
    /// </returns>
    /// <response code="200">Returns true if the username is available, false if already in use.</response>
    /// <response code="400">The provided username is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to check username availability.</response>
    /// <response code="500">An internal server error occurred during the check.</response>
    /// <remarks>
    /// This endpoint allows anonymous access to facilitate user registration processes.
    /// Returns true when the username is ready to use (not taken by another user).
    /// </remarks>
    /// <example>
    /// GET /api/user/username-availability/?username=newuser123
    /// </example>
    [HttpGet("username-availability/", Name = "IsUsernameAvailable")]
    [ProducesResponseType(typeof(bool),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> IsUsernameAvailable([FromQuery] string username, CancellationToken cancellationToken = default)
    {
        var isAvailable = await userService.IsUsernameAvailableAsync(username, cancellationToken);
        return Ok(isAvailable);
    }
}
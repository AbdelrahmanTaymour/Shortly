using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.ServiceContracts;
using Shortly.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Shortly.API.Controllers;

/// <summary>
///     Admin controller for user management operations.
///     Provides endpoints for user administration with comprehensive permissions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Admin Operations", Description = "Administrative endpoints for user management")]
public class AdminController(IUserService userService) : ControllerBase
{
    /// <summary>
    ///     Search users with filtering and pagination.
    /// </summary>
    /// <param name="searchTerm">Search term for user name or email</param>
    /// <param name="role">Filter by user role</param>
    /// <param name="subscriptionPlan">Filter by subscription plan</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated search results</returns>
    [HttpGet("users/search", Name = "SearchUsers")]
    [RequirePermission(enPermissions.ViewAllUsers)]
    [ProducesResponseType(typeof(UserSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Search users",
        Description = "Search users with filtering and pagination options",
        OperationId = "SearchUsers"
    )]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? searchTerm = null,
        [FromQuery] enUserRole? role = null,
        [FromQuery] enSubscriptionPlan? subscriptionPlan = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var response = await userService.SearchUsers(searchTerm, role, subscriptionPlan, isActive, page, pageSize);
        return Ok(response);
    }
    
    /// <summary>
    ///     Get all users in the system.
    /// </summary>
    /// <returns>List of all users</returns>
    [HttpGet("users", Name = "GetAllUsers")]
    [RequirePermission(enPermissions.ViewAllUsers)]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Get all users",
        Description = "Retrieves all users in the system",
        OperationId = "GetAllUsers"
    )]
    public async Task<IActionResult> GetAll()
    {
        var users = await userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    ///     Get user by ID.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User profile details</returns>
    [HttpGet("users/{id:guid}", Name = "GetUserById")]
    [RequirePermission(enPermissions.SearchUsers)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Get user by ID",
        Description = "Retrieves user profile by user ID",
        OperationId = "GetUserById"
    )]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    ///     Create a new user account.
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user profile</returns>
    [HttpPost("users", Name = "AddNewUser")]
    [RequirePermission(enPermissions.AddUser)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Create new user",
        Description = "Creates a new user account",
        OperationId = "AddNewUser"
    )]
    public async Task<IActionResult> AddUser([FromBody] CreateUserRequest request)
    {
        var response = await userService.CreateUserAsync(request);
        return CreatedAtAction("GetUserById", new { id = response.Id }, response);
    }

    /// <summary>
    ///     Update user profile.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateUser">Updated user data</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("users/{id:guid}", Name = "UpdateUser")]
    [RequirePermission(enPermissions.UpdateUser)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Update user",
        Description = "Updates user profile information",
        OperationId = "UpdateUser"
    )]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUser)
    {
        var response = await userService.UpdateUserAsync(id, updateUser);
        if (response == null) return NotFound();
        return Ok(response);
    }

    /// <summary>
    ///     Soft delete user account.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("users/soft-delete/{id:guid}", Name = "SoftDeleteUser")]
    [RequirePermission(enPermissions.SoftDeleteUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Soft delete user",
        Description = "Soft deletes a user account (marks as deleted but preserves data)",
        OperationId = "SoftDeleteUser"
    )]
    public async Task<IActionResult> SoftDeleteUser(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var isDeleted = await userService.SoftDeleteUserAccount(id, currentUserId);
        if (!isDeleted) return NotFound();
        return NoContent();
    }

    /// <summary>
    ///     Hard delete user account.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("users/hard-delete/{id:guid}", Name = "HardDeleteUser")]
    [RequirePermission(enPermissions.HardDeleteUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Hard delete user",
        Description = "Permanently deletes a user account and all associated data",
        OperationId = "HardDeleteUser"
    )]
    public async Task<IActionResult> HardDeleteUser(Guid id)
    {
        var isDeleted = await userService.HardDeleteUserAsync(id);
        if (!isDeleted) return NotFound();
        return NoContent();
    }

    /// <summary>
    ///     Lock user account.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="lockUntil">Lock until date (optional)</param>
    /// <returns>Success message</returns>
    [HttpPost("users/lock-user/{id:guid}", Name = "LockUser")]
    [RequirePermission(enPermissions.LockUserAccounts)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Lock user account",
        Description = "Locks a user account temporarily or permanently",
        OperationId = "LockUser"
    )]
    public async Task<IActionResult> LockUser(Guid id, [FromBody] DateTime? lockUntil = null)
    {
        var success = await userService.LockUser(id, lockUntil);
        if (!success) return BadRequest();
        return Ok(new { message = "User account locked successfully." });
    }
    
    /// <summary>
    ///     Unlock user account.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success message</returns>
    [HttpPost("users/unlock-user/{id:guid}", Name = "UnlockUser")]
    [RequirePermission(enPermissions.LockUserAccounts)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Unlock user account",
        Description = "Unlocks a previously locked user account",
        OperationId = "UnlockUser"
    )]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var success = await userService.UnlockUser(id);
        if (!success) return BadRequest();
        return Ok(new { message = "User account unlocked successfully." });
    }
    
    /// <summary>
    ///     Activate user account.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success message</returns>
    [HttpPost("users/activate-user/{id:guid}", Name = "ActivateUser")]
    [RequirePermission(enPermissions.LockUserAccounts)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Activate user account",
        Description = "Activates a deactivated user account",
        OperationId = "ActivateUser"
    )]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        var success = await userService.ActivateUser(id);
        if (!success) return BadRequest();
        return Ok(new { message = "User account activated successfully." });
    }
    
    /// <summary>
    ///     Deactivate user account.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success message</returns>
    [HttpPost("users/deactivate-user/{id:guid}", Name = "DeactivateUser")]
    [RequirePermission(enPermissions.LockUserAccounts)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Deactivate user account",
        Description = "Deactivates an active user account",
        OperationId = "DeactivateUser"
    )]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var success = await userService.DeactivateUser(id);
        if (!success) return BadRequest();
        return Ok(new { message = "User account deactivated successfully." });
    }

    /// <summary>
    ///     Get user analytics and statistics.
    /// </summary>
    /// <returns>User analytics data</returns>
    [HttpGet("users/analytics", Name = "GetUserAnalytics")]
    [RequirePermission(enPermissions.ViewUsersAnalytics)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Get user analytics",
        Description = "Retrieves user analytics and statistics",
        OperationId = "GetUserAnalytics"
    )]
    public async Task<IActionResult> GetUserAnalytics()
    {
        // TODO: Implement user analytics
        throw new NotImplementedException("User analytics not yet implemented");
    }
    
    #region Private Methods

    /// <summary>
    ///     Get the current user ID from claims.
    /// </summary>
    /// <returns>Current user ID or empty GUID if not found</returns>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    #endregion
}
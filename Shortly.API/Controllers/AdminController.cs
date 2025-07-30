using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using MethodTimer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.ServiceContracts;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController(IUserService userService) : ControllerBase
{
    
    // TODO: THIS CONTROLLER NEEDS ENHANCEMENT

    /// <summary>
    /// Searches for users based on filters like role, subscription, or status.
    /// </summary>
    [Time]
    [HttpGet("users/search", Name = "SearchUsers")]
    [RequirePermission(enPermissions.ViewAllUsers)]
    [ProducesResponseType(typeof(UserSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? searchTerm = null,
        [FromQuery] enUserRole? role = null,
        [FromQuery] enSubscriptionPlan? subscriptionPlan = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var response = await userService
            .SearchUsers(searchTerm, role, subscriptionPlan, isActive, page, pageSize);
        return Ok(response);
    }
    
    /// <summary>
    /// Retrieves all users.
    /// </summary>
    [Time]
    [HttpGet("users", Name = "GetAllUsers")]
    [RequirePermission(enPermissions.ViewAllUsers)]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll()
    {
        var users = await userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Gets user details by ID.
    /// </summary>
    [Time]
    [HttpGet("users/{id:guid}", Name = "GetUserById")]
    [RequirePermission(enPermissions.SearchUsers)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);
        return Ok(user);
    }

    /// <summary>
    /// Adds a new user.
    /// </summary>
    [Time]
    [HttpPost("users", Name = "AddNewUser")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.AddUser)]
    public async Task<IActionResult> AddUser([FromBody] CreateUserRequest request)
    {
        var response = await userService.CreateUserAsync(request);
        return CreatedAtAction("GetUserById", new { id = response.Id }, response);
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    [Time]
    [HttpPut("users/{id:guid}", Name = "UpdateUser")]
    [RequirePermission(enPermissions.UpdateUser)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUser)
    {
        var response = await userService.UpdateUserAsync(id, updateUser);
        return Ok(response);
    }

    /// <summary>
    /// Soft deletes a user.
    /// </summary>
    [Time]
    [HttpDelete("users/soft-delete/{id:guid}", Name = "SoftDeleteUser")]
    [RequirePermission(enPermissions.SoftDeleteUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SoftDeleteUser(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var isDeleted = await userService.SoftDeleteUserAccount(id, currentUserId);
        return isDeleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Hard deletes a user.
    /// </summary>
    [Time]
    [HttpDelete("users/hard-delete/{id:guid}", Name = "HardDeleteUser")]
    [RequirePermission(enPermissions.HardDeleteUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HardDeleteUser(Guid id)
    {
        var isDeleted = await userService.HardDeleteUserAsync(id);
        return isDeleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Locks a user account until a given date.
    /// </summary>
    [Time]
    [HttpPost("Users/lock-user", Name = "LockUser")]
    [RequirePermission(enPermissions.LockUserAccounts)]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LockUser(Guid id, [FromBody] DateTime? lockUntil = null)
    {
        var success = await userService.LockUser(id, lockUntil);
        return success
            ? Ok(new { message = "User account locked successfully." })
            : BadRequest();
    }
    
    /// <summary>
    /// Unlocks a user account.
    /// </summary>
    [Time]
    [HttpPost("Users/unlock-user", Name = "UnlockUser")]
    [RequirePermission(enPermissions.LockUserAccounts)]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var success = await userService.UnlockUser(id);
        if(!success)
        {
            return BadRequest();
        }
        return Ok(new {message = "User account unlocked successfully." });
    }
    
    /// <summary>
    /// Activates a user account.
    /// </summary>
    [Time]
    [HttpPost("Users/activate-user", Name = "ActivateUser")]
    [RequirePermission(enPermissions.LockUserAccounts)]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        var success = await userService.ActivateUser(id);
        return success
            ? Ok(new { message = "User account activated successfully." })
            : BadRequest();
    }
    
    /// <summary>
    /// Deactivates a user account.
    /// </summary>
    [Time]
    [HttpPost("Users/deactivate-user", Name = "DeactivateUser")]
    [RequirePermission(enPermissions.LockUserAccounts)]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var success = await userService.DeactivateUser(id);
        return success
            ? Ok(new { message = "User account deactivate successfully." })
            : BadRequest();
    }

    /// <summary>
    /// Retrieves analytics related to users.
    /// </summary>
    [Time]
    [HttpGet("users/analytics", Name = "GetUserAnalytics")]
    [RequirePermission(enPermissions.ViewUsersAnalytics)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserAnalytics()
    {
        // TODO: CREATE A COMPREHENSIVE USER Analytics
        throw new NotImplementedException();
    }
    
    #region Private

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    #endregion
}
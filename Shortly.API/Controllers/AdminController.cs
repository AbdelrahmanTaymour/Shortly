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

    [Time]
    [HttpGet("users/search", Name = "SearchUsers")]
    [RequirePermission(enPermissions.ViewAllUsers)]
    [ProducesResponseType(typeof(UserSearchResponse), StatusCodes.Status200OK)]
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
    
    [Time]
    [HttpGet("users", Name = "GetAllUsers")]
    [RequirePermission(enPermissions.ViewAllUsers)]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var users = await userService.GetAllUsersAsync();
        return Ok(users);
    }

    [Time]
    [HttpGet("users/{id:guid}", Name = "GetUserById")]
    [RequirePermission(enPermissions.SearchUsers)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);
        return Ok(user);
    }

    [Time]
    [HttpPost("users", Name = "AddUser")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [RequirePermission(enPermissions.AddUser)]
    public async Task<IActionResult> AddUser([FromBody] CreateUserRequest request)
    {
        var response = await userService.CreateUserAsync(request);
        return CreatedAtAction("GetUserById", new { id = response.Id }, response);
    }

    [Time]
    [HttpPut("users/{id:guid}", Name = "UpdateUser")]
    [RequirePermission(enPermissions.UpdateUser)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUser)
    {
        var response = await userService.UpdateUserAsync(id, updateUser);
        return Ok(response);
    }

    [Time]
    [HttpDelete("users/soft-delete/{id:guid}")]
    [RequirePermission(enPermissions.SoftDeleteUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDeleteUser(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var isDeleted = await userService.SoftDeleteUserAccount(id, currentUserId);
        return isDeleted ? NoContent() : NotFound();
    }

    [Time]
    [HttpDelete("users/hard-delete/{id:guid}")]
    [RequirePermission(enPermissions.HardDeleteUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HardDeleteUser(Guid id)
    {
        var isDeleted = await userService.HardDeleteUserAsync(id);
        return isDeleted ? NoContent() : NotFound();
    }


    #region Private

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    #endregion
}
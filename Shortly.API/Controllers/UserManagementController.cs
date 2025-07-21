using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Attributes;
using Shortly.Core.Constants;
using Shortly.Core.DTOs;
using Shortly.Core.Entities;
using Shortly.Core.ServiceContracts;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly IRoleManagementService _roleManagementService;
    private readonly IAuthorizationService _authorizationService;

    public UserManagementController(
        IUserManagementService userManagementService,
        IRoleManagementService roleManagementService,
        IAuthorizationService authorizationService)
    {
        _userManagementService = userManagementService;
        _roleManagementService = roleManagementService;
        _authorizationService = authorizationService;
    }

    #region User CRUD Operations

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [RequirePermission(DefaultPermissions.CreateUsers)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _userManagementService.CreateUserAsync(request);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, 
                new ApiResponse<UserResponse>(true, user, "User created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse("Validation Error", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiErrorResponse("Internal Error", ex.Message));
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(DefaultPermissions.ReadUsers)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        
        // Users can always view their own profile
        if (currentUserId != id)
        {
            var canView = await _authorizationService.CanManageUserAsync(currentUserId, id);
            if (!canView)
            {
                return Forbid();
            }
        }

        var user = await _userManagementService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new ApiErrorResponse("Not Found", "User not found"));
        }

        return Ok(new ApiResponse<UserResponse>(true, user));
    }

    /// <summary>
    /// Search and list users with pagination
    /// </summary>
    [HttpGet]
    [RequirePermission(DefaultPermissions.ReadUsers)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers([FromQuery] UserSearchRequest request)
    {
        var users = await _userManagementService.GetUsersAsync(request);
        return Ok(new ApiResponse<PagedResult<UserResponse>>(true, users));
    }

    /// <summary>
    /// Update user information
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(DefaultPermissions.UpdateUsers)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var currentUserId = GetCurrentUserId();
        
        // Users can update their own profile, others need permission
        if (currentUserId != id)
        {
            var canUpdate = await _authorizationService.CanManageUserAsync(currentUserId, id);
            if (!canUpdate)
            {
                return Forbid();
            }
        }

        try
        {
            var user = await _userManagementService.UpdateUserAsync(id, request);
            return Ok(new ApiResponse<UserResponse>(true, user, "User updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse("Validation Error", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiErrorResponse("Internal Error", ex.Message));
        }
    }

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(DefaultPermissions.DeleteUsers)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(Guid id, [FromQuery] string reason = "Deleted by administrator")
    {
        var currentUserId = GetCurrentUserId();
        
        // Prevent self-deletion
        if (currentUserId == id)
        {
            return BadRequest(new ApiErrorResponse("Invalid Operation", "You cannot delete your own account"));
        }

        var result = await _userManagementService.DeleteUserAsync(id, reason);
        if (!result)
        {
            return NotFound(new ApiErrorResponse("Not Found", "User not found"));
        }

        return Ok(new ApiResponse<bool>(true, true, "User deleted successfully"));
    }

    #endregion

    #region User Status Management

    /// <summary>
    /// Activate user account
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [RequirePermission(DefaultPermissions.UpdateUsers)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        var result = await _userManagementService.ActivateUserAsync(id);
        if (!result)
        {
            return NotFound(new ApiErrorResponse("Not Found", "User not found"));
        }

        return Ok(new ApiResponse<bool>(true, true, "User activated successfully"));
    }

    /// <summary>
    /// Suspend user account
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [RequirePermission(DefaultPermissions.BanUsers)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendUser(Guid id, [FromBody] SuspendUserRequest request)
    {
        var result = await _userManagementService.SuspendUserAsync(id, request.Reason, request.Until);
        if (!result)
        {
            return NotFound(new ApiErrorResponse("Not Found", "User not found"));
        }

        return Ok(new ApiResponse<bool>(true, true, "User suspended successfully"));
    }

    /// <summary>
    /// Ban user account
    /// </summary>
    [HttpPost("{id:guid}/ban")]
    [RequirePermission(DefaultPermissions.BanUsers)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanUser(Guid id, [FromBody] BanUserRequest request)
    {
        var result = await _userManagementService.BanUserAsync(id, request.Reason);
        if (!result)
        {
            return NotFound(new ApiErrorResponse("Not Found", "User not found"));
        }

        return Ok(new ApiResponse<bool>(true, true, "User banned successfully"));
    }

    #endregion

    #region Role Management

    /// <summary>
    /// Get user's roles
    /// </summary>
    [HttpGet("{id:guid}/roles")]
    [RequirePermission(DefaultPermissions.ViewRoles)]
    [ProducesResponseType(typeof(ApiResponse<List<RoleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(Guid id)
    {
        var roles = await _roleManagementService.GetUserRolesAsync(id);
        return Ok(new ApiResponse<List<RoleResponse>>(true, roles));
    }

    /// <summary>
    /// Assign roles to user
    /// </summary>
    [HttpPost("{id:guid}/roles")]
    [RequirePermission(DefaultPermissions.AssignRoles)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRolesToUser(Guid id, [FromBody] RoleAssignmentRequest request)
    {
        var currentUserId = GetCurrentUserId();
        
        try
        {
            var result = await _roleManagementService.SetUserRolesAsync(id, request.RoleIds, currentUserId);
            if (!result)
            {
                return BadRequest(new ApiErrorResponse("Assignment Failed", "Failed to assign roles to user"));
            }

            return Ok(new ApiResponse<bool>(true, true, "Roles assigned successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse("Validation Error", ex.Message));
        }
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [RequirePermission(DefaultPermissions.AssignRoles)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveRoleFromUser(Guid id, Guid roleId)
    {
        var result = await _roleManagementService.RemoveRoleFromUserAsync(id, roleId);
        return Ok(new ApiResponse<bool>(true, result, result ? "Role removed successfully" : "Role removal failed"));
    }

    #endregion

    #region User Activity & Sessions

    /// <summary>
    /// Get user's activity history
    /// </summary>
    [HttpGet("{id:guid}/activity")]
    [RequirePermission(DefaultPermissions.ViewUserActivity)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserActivityResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserActivity(Guid id, [FromQuery] UserActivitySearchRequest request)
    {
        var activity = await _userManagementService.GetUserActivityAsync(id, request);
        return Ok(new ApiResponse<PagedResult<UserActivityResponse>>(true, activity));
    }

    /// <summary>
    /// Get user's active sessions
    /// </summary>
    [HttpGet("{id:guid}/sessions")]
    [RequirePermission(DefaultPermissions.ViewUserActivity)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserSessionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserSessions(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var sessions = await _userManagementService.GetUserSessionsAsync(id, page, pageSize);
        return Ok(new ApiResponse<PagedResult<UserSessionResponse>>(true, sessions));
    }

    /// <summary>
    /// Terminate user session
    /// </summary>
    [HttpDelete("sessions/{sessionId:guid}")]
    [RequirePermission(DefaultPermissions.ViewUserActivity)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TerminateSession(Guid sessionId)
    {
        var result = await _userManagementService.TerminateSessionAsync(sessionId);
        return Ok(new ApiResponse<bool>(true, result, result ? "Session terminated" : "Session not found"));
    }

    /// <summary>
    /// Terminate all user sessions
    /// </summary>
    [HttpDelete("{id:guid}/sessions")]
    [RequirePermission(DefaultPermissions.ViewUserActivity)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TerminateAllUserSessions(Guid id)
    {
        var result = await _userManagementService.TerminateAllUserSessionsAsync(id);
        return Ok(new ApiResponse<bool>(true, result, "All sessions terminated"));
    }

    /// <summary>
    /// Get user login history
    /// </summary>
    [HttpGet("{id:guid}/login-history")]
    [RequirePermission(DefaultPermissions.ViewUserActivity)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserLoginHistoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoginHistory(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var history = await _userManagementService.GetLoginHistoryAsync(id, page, pageSize);
        return Ok(new ApiResponse<PagedResult<UserLoginHistoryResponse>>(true, history));
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Perform bulk operations on multiple users
    /// </summary>
    [HttpPost("bulk")]
    [RequireAdmin]
    [ProducesResponseType(typeof(ApiResponse<BulkUserOperationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUserOperation([FromBody] BulkUserOperationRequest request)
    {
        var results = new List<BulkOperationResult>();
        int successCount = 0;

        foreach (var userId in request.UserIds)
        {
            try
            {
                bool success = request.Operation.ToLower() switch
                {
                    "activate" => await _userManagementService.ActivateUserAsync(userId),
                    "suspend" => await _userManagementService.SuspendUserAsync(userId, request.Reason ?? "Bulk operation", request.ExpiresAt),
                    "ban" => await _userManagementService.BanUserAsync(userId, request.Reason ?? "Bulk operation"),
                    "delete" => await _userManagementService.DeleteUserAsync(userId, request.Reason ?? "Bulk operation"),
                    _ => false
                };

                if (success) successCount++;
                results.Add(new BulkOperationResult(userId, success));
            }
            catch (Exception ex)
            {
                results.Add(new BulkOperationResult(userId, false, ex.Message));
            }
        }

        var response = new BulkUserOperationResponse(
            request.UserIds.Count,
            successCount,
            request.UserIds.Count - successCount,
            results
        );

        return Ok(new ApiResponse<BulkUserOperationResponse>(true, response, $"Bulk operation completed: {successCount}/{request.UserIds.Count} successful"));
    }

    #endregion

    #region Statistics & Analytics

    /// <summary>
    /// Get user statistics
    /// </summary>
    [HttpGet("{id:guid}/statistics")]
    [RequirePermission(DefaultPermissions.ViewUserActivity)]
    [ProducesResponseType(typeof(ApiResponse<UserStatisticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserStatistics(Guid id)
    {
        var statistics = await _userManagementService.GetUserStatisticsAsync(id);
        return Ok(new ApiResponse<UserStatisticsResponse>(true, statistics));
    }

    #endregion

    #region Helper Methods

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    #endregion
}

// Supporting DTOs for specific operations
public record SuspendUserRequest(string Reason, DateTime? Until = null);
public record BanUserRequest(string Reason);
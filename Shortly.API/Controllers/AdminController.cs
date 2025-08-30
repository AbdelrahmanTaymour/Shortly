using MethodTimer;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.Models;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

/// <summary>
///     Administrative controller for managing users in the system.
///     Provides endpoints for user search, updates, deletions, and bulk operations.
/// </summary>
/// <remarks>
///     All endpoints in this controller require appropriate administrative permissions.
/// </remarks>
[ApiController]
[Route("api/admin")]
public class AdminController(
    IUserQueryService queryService, 
    IUserAdministrationService adminService) : ControllerApiBase
{
    /// <summary>
    ///     Searches for users and returns basic information with pagination support.
    /// </summary>
    /// <param name="request">The search criteria and pagination parameters</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>A paginated list of basic user information matching the search criteria</returns>
    /// <response code="200">Returns the paginated search results</response>
    /// <response code="400">Invalid search parameters provided</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User lacks permission to view all users</response>
    /// <response code="500">Internal server error occurred</response>
    /// <example>
    /// GET /api/admin/search/basic?searchTerm=john&amp;page=1&amp;pageSize=10
    /// </example>
    [HttpGet("search/basic", Name = "SearchBasicUsers")]
    [ProducesResponseType(typeof(BasicUserSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewUsers)]
    [Time]
    public async Task<ActionResult<BasicUserSearchResponse>> SearchBasicUsers(
        [FromQuery] UserSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.SearchBasicUsersAsync(request, cancellationToken);
        return Ok(result);
    }


    /// <summary>
    ///     Searches for users and returns complete information including profile, security, and usage data.
    /// </summary>
    /// <param name="request">The search criteria and pagination parameters</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>A paginated list of complete user information with all related data</returns>
    /// <response code="200">Returns the complete paginated search results</response>
    /// <response code="400">Invalid search parameters provided</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User lacks permission to view all users</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    ///     This endpoint returns comprehensive user data and should be used when detailed information is required.
    ///     For better performance with large datasets, consider using the basic search endpoint instead.
    /// </remarks>
    /// <example>
    /// GET /api/admin/search/complete?searchTerm=admin&amp;includeInactive=true&amp;page=1&amp;pageSize=5
    /// </example>
    [HttpGet("search/complete", Name = "SearchCompleteUsers")]
    [ProducesResponseType(typeof(CompleteUserSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewUserDetails)]
    public async Task<ActionResult<CompleteUserSearchResponse>> SearchCompleteUsers(
        [FromQuery] UserSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.SearchCompleteUsersAsync(request, cancellationToken);
        return Ok(result);
    }


    /// <summary>
    ///     Forces an update on a specific user, bypassing normal validation rules.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update</param>
    /// <param name="request">The force update request containing the changes to apply</param>
    /// <returns>The updated user information</returns>
    /// <response code="200">User successfully updated</response>
    /// <response code="400">Invalid user ID or update request</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User lacks permission to update users</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    ///     This is a privileged operation that bypasses standard validation rules. Use with caution as it can override system
    ///     constraints. All operations are logged for audit purposes.
    /// </remarks>
    /// <example>
    /// POST /api/admin/users/force-update/12345678-1234-1234-1234-123456789012
    /// </example>
    [HttpPut("users/force-update/{userId:guid:required}", Name = "ForceUpdateUser")]
    [ProducesResponseType(typeof(ForceUpdateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.UpdateUser)]
    public async Task<IActionResult> ForceUpdateUser(Guid userId, [FromBody] ForceUpdateUserRequest request)
    {
        var updatedUser = await adminService.ForceUpdateUserAsync(userId, request);
        return Ok(updatedUser);
    }


    /// <summary>
    ///     Permanently deletes a user and all associated data from the system.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete</param>
    /// <param name="deleteOwnedShortUrls">Whether to delete short URLs owned by the user (default: false)</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>No content on successful deletion</returns>
    /// <response code="204">User successfully deleted</response>
    /// <response code="400">Invalid user ID provided</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User lacks permission to hard delete users</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    ///     WARNING: This operation is irreversible and will permanently delete the user and all associated data. Consider
    ///     using soft delete instead unless permanent deletion is absolutely necessary.
    ///     When deleteOwnedShortUrls is false, short URLs will be reassigned or made anonymous. When true, they will be
    ///     permanently deleted.
    ///     Example usage: DELETE /api/admin/users/hard-delete/12345678-1234-1234-1234-123456789012?deleteOwnedShortUrls=true
    /// </remarks>
    /// <example>
    /// DELETE /api/admin/users/hard-delete/12345678-1234-1234-1234-123456789012?deleteOwnedShortUrls=true
    /// </example>
    [HttpDelete("users/hard-delete/{userId:guid:required}", Name = "HardDeleteUser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.DeleteUser)]
    public async Task<IActionResult> HardDeleteUser(
        Guid userId,
        bool deleteOwnedShortUrls = false,
        CancellationToken cancellationToken = default)
    {
        await adminService.HardDeleteUserAsync(userId, deleteOwnedShortUrls, cancellationToken);
        return NoContent();
    }


    /// <summary>
    ///     Activates multiple users in a single batch operation.
    /// </summary>
    /// <param name="userIds">Collection of user IDs to activate</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>Result summary of the bulk activation operation</returns>
    /// <response code="200">Bulk operation completed with results</response>
    /// <response code="400">Invalid user IDs or empty collection provided</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User lacks permission to activate users</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    ///     This operation will attempt to activate all provided users. The response will include details about successful and
    ///     failed operations. Partial success is possible - some users may be activated while others fail.
    /// </remarks>
    /// <example>
    /// POST /api/admin/users/bulk-activate
    /// Body: ["12345678-1234-1234-1234-123456789012", "87654321-4321-4321-4321-210987654321"]
    /// </example>
    [HttpPut("users/bulk-activate", Name = "BulkActivateUsers")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.UpdateUser)]
    public async Task<IActionResult> BulkActivateUsers(
        [FromBody] ICollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = await adminService.BulkActivateUsersAsync(userIds, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Deactivates multiple users in a single batch operation.
    /// </summary>
    /// <param name="userIds">Collection of user IDs to deactivate</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>Result summary of the bulk deactivation operation</returns>
    /// <response code="200">Bulk operation completed with results</response>
    /// <response code="400">Invalid user IDs or empty collection provided</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User lacks permission to deactivate users</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    ///     This operation will attempt to deactivate all provided users. Deactivated users will lose access to the system, but
    ///     their data will be preserved. The response includes details about successful and failed operations.
    /// </remarks>
    [HttpPut("users/bulk-deactivate", Name = "BulkDeactivateUsers")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.UpdateUser)]
    public async Task<IActionResult> BulkDeactivateUsers(
        [FromBody] ICollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = await adminService.BulkDeactivateUsersAsync(userIds, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Soft deletes multiple users in a single batch operation.
    /// </summary>
    /// <param name="userIds">Collection of user IDs to softly delete</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>Result summary of the bulk deletion operation</returns>
    /// <response code="200">Bulk operation completed with results</response>
    /// <response code="400">Invalid user IDs or empty collection provided</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User lacks permission to delete users or unable to determine current admin ID</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    ///     This operation performs soft deletion, marking users as deleted while
    ///     preserving their data for potential recovery. The operation is audited
    ///     with the current administrator's ID for accountability.
    ///     Users will be immediately inaccessible, but data can be recovered if needed.
    /// </remarks>
    /// <example>
    /// POST /api/admin/users/bulk-deactivate
    /// Body: ["12345678-1234-1234-1234-123456789012", "87654321-4321-4321-4321-210987654321"]
    /// </example>
    [HttpDelete("users/bulk-delete", Name = "BulkDeleteUsers")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.DeleteUser)]
    public async Task<IActionResult> BulkDeleteUsers(
        [FromBody] ICollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var currentAdminId = GetCurrentUserId();
        var result = await adminService.BulkDeleteUsersAsync(userIds, currentAdminId, cancellationToken);
        return Ok(result);
    }
}
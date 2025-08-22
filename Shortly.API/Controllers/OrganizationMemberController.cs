using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;


/// <summary>
/// Controller for managing organization members, providing CRUD operations and membership management functionality.
/// </summary>
/// <remarks>
/// This controller handles adding, removing, updating roles and permissions of organization members,
/// as well as retrieving member information and checking membership status.
/// </remarks>
[ApiController]
[Route("api/organization-members")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class OrganizationMemberController(IOrganizationMemberService memberService) : ControllerApiBase
{
    /// <summary>
    /// Retrieves all organization members with pagination support.
    /// </summary>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of organization members.</returns>
    /// <example>GET /api/organization-members?page=1&amp;pageSize=10</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-members?page=1&amp;pageSize=10
    /// </remarks>
    /// <response code="200">Returns the list of organization members.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read organization members.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet(Name = "GetAllOrganizationMembers")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewMembers)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var members = await memberService.GetAllMembersAsync(page, pageSize, cancellationToken);
        return Ok(members);
    }
    
    /// <summary>
    /// Retrieves all members of a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of organization members.</returns>
    /// <example>GET /api/organization-members/organization/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-members/organization/550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    /// <response code="200">Returns the list of organization members.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read organization members.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("organization/{organizationId:guid}", Name = "GetOrganizationMembers")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewMembers)]
    public async Task<IActionResult> GetOrganizationMembers(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var members = await memberService.GetOrganizationMembersAsync(organizationId, cancellationToken);
        return Ok(members);
    }
    
    /// <summary>
    /// Retrieves all organization memberships for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of user's organization memberships.</returns>
    /// <example>GET /api/organization-members/user/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-members/user/550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    /// <response code="200">Returns the list of user's organization memberships.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read organization members.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("user/{userId:guid}", Name = "GetUserMemberships")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewUserMemberships)]
    public async Task<IActionResult> GetUserMemberships(Guid userId, CancellationToken cancellationToken = default)
    {
        var memberships = await memberService.GetUserMembershipsAsync(userId, cancellationToken);
        return Ok(memberships);
    }
    
    /// <summary>
    /// Retrieves a specific membership for a user in an organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The membership details if found.</returns>
    /// <example>GET /api/organization-members/550e8400-e29b-41d4-a716-446655440000/user/660f9511-f3ac-52e5-b827-557766551111</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-members/550e8400-e29b-41d4-a716-446655440000/user/660f9511-f3ac-52e5-b827-557766551111
    /// </remarks>
    /// <response code="200">Returns the membership details.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read organization members.</response>
    /// <response code="404">Membership is not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{organizationId:guid}/user/{userId:guid}", Name = "GetMembership")]
    [ProducesResponseType(typeof(OrganizationMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewMembers)]
    public async Task<IActionResult> GetMembership(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var membership = await memberService.GetMembershipAsync(organizationId, userId, cancellationToken);
        return Ok(membership);
    }
    
     /// <summary>
    /// Adds a new member to an organization.
    /// </summary>
    /// <param name="request">The member data to add.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created membership details.</returns>
    /// <example>POST /api/organization-members</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-members
    ///      {
    ///         "organizationId": "550e8400-e29b-41d4-a716-446655440000",
    ///         "userId": "660f9511-f3ac-52e5-b827-557766551111",
    ///         "roleId": "Member",
    ///         "invitedBy": "770g0622-g4bd-63f6-c938-668877662222"
    ///      }
    /// </remarks>
    /// <response code="201">Member added successfully.</response>
    /// <response code="400">Invalid request data or organization has reached member limit.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to add members.</response>
    /// <response code="404">Organization not found.</response>
    /// <response code="409">User is already a member of the organization.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost(Name = "ManageMembers")]
    [ProducesResponseType(typeof(OrganizationMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
     [RequirePermission(enPermissions.ManageOrgMembers)]
    public async Task<IActionResult> AddMember([FromBody] CreateMemberRequest request, CancellationToken cancellationToken = default)
    {
        var member = await memberService.AddMemberAsync(request, cancellationToken);

        return CreatedAtRoute("GetMembership", new { organizationId = member.OrganizationId, userId = member.UserId },
            member);
    }
     
    /// <summary>
    /// Removes a member from an organization. Organization owners cannot be removed.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user to remove.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the removal operation.</returns>
    /// <example>DELETE /api/organization-members/550e8400-e29b-41d4-a716-446655440000/user/660f9511-f3ac-52e5-b827-557766551111</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      DELETE /api/organization-members/550e8400-e29b-41d4-a716-446655440000/user/660f9511-f3ac-52e5-b827-557766551111
    /// </remarks>
    /// <response code="204">Member removed successfully.</response>
    /// <response code="400">Cannot remove an organization owner.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to remove members.</response>
    /// <response code="404">Member not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpDelete("{organizationId:guid}/user/{userId:guid}", Name = "ViewUserMemberships")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.RemoveMembers)]
    public async Task<IActionResult> RemoveMember(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var requestingUserId = GetCurrentUserId();
        await memberService.RemoveMemberAsync(organizationId, userId, requestingUserId, cancellationToken);
        return NoContent();
    }
    
     /// <summary>
    /// Updates the role of an organization member.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="updateRoleDto">The new role data.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the role update operation.</returns>
    /// <example>PUT /api/organization-members/550e8400-e29b-41d4-a716-446655440000/user/660f9511-f3ac-52e5-b827-557766551111/role</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      PUT /api/organization-members/550e8400-e29b-41d4-a716-446655440000/user/660f9511-f3ac-52e5-b827-557766551111/role
    ///      {
    ///         "roleId": "SuperAdmin"
    ///      }
    /// </remarks>
    /// <response code="200">Role updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update member roles.</response>
    /// <response code="404">Member not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPut("{organizationId:guid}/user/{userId:guid}/role", Name = "UpdateMemberRole")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ManageOrgMembers)]
    public async Task<IActionResult> UpdateMemberRole(Guid organizationId, Guid userId, [FromBody] UpdateMemberRoleRequest updateRoleDto, CancellationToken cancellationToken = default)
    {
        var result = await memberService.UpdateMemberRoleAsync(organizationId, userId, updateRoleDto.RoleId, cancellationToken);
        return Ok(result);
    }
     
    /// <summary>
    /// Updates the permissions of an organization member.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="updatePermissionsDto">The new permissions data.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the permissions update operation.</returns>
    /// <example>PUT /api/organization-members/550e8400-e29b-41d4-a716-446655440000/user/660f9511-f3ac-52e5-b827-557766551111/permissions</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      PUT /api/organization-members/550e8400-e29b-41d4-a716-446655440000/user/660f9511-f3ac-52e5-b827-557766551111/permissions
    ///      {
    ///         "permissions": "ReadWrite"
    ///      }
    /// </remarks>
    /// <response code="200">Permissions updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update member permissions.</response>
    /// <response code="404">Member not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPut("{organizationId:guid}/user/{userId:guid}/permissions", Name = "UpdateMemberPermissions")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ManageOrgMembers)]
    public async Task<IActionResult> UpdateMemberPermissions(Guid organizationId, Guid userId, [FromBody] UpdateMemberPermissionsRequest updatePermissionsDto, CancellationToken cancellationToken = default)
    {
        var result = await memberService.UpdateMemberPermissionsAsync(organizationId, userId, updatePermissionsDto.Permissions, cancellationToken);
        return Ok(result);
    }
    
    /// <summary>
    /// Checks if a user is a member of an organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the user is a member, false otherwise.</returns>
    /// <example>GET /api/organization-members/550e8400-e29b-41d4-a716-446655440000/user/660f9511-f3ac-52e5-b827-557766551111/is-member</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-members/550e8400-e29b-41d4-a716-446655440000/user/660f9511-f3ac-52e5-b827-557766551111/is-member
    /// </remarks>
    /// <response code="200">Returns the membership status.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to check membership status.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{organizationId:guid}/user/{userId:guid}/is-member", Name = "IsMember")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewMembers)]
    public async Task<IActionResult> IsMember(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var isMember = await memberService.IsMemberAsync(userId, organizationId, cancellationToken);
        return Ok(isMember);
    }
}
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;


/// <summary>
/// Controller for managing organization teams, providing CRUD operations and team management functionality.
/// </summary>
/// <remarks>
/// This controller handles creating, updating, deleting teams within organizations,
/// managing team members, and changing team managers.
/// </remarks>
[ApiController]
[Route("api/organization-teams")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class OrganizationTeamController(IOrganizationTeamService teamService) : ControllerApiBase
{
    /// <summary>
    /// Retrieves a team by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the team.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The team details if found.</returns>
    /// <example>GET /api/organization-teams/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-teams/550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    /// <response code="200">Returns the team details.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read teams.</response>
    /// <response code="404">The Team with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{id:guid}", Name = "GetTeamById")]
    [ProducesResponseType(typeof(OrganizationTeamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewTeams)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await teamService.GetTeamAsync(id, cancellationToken);
        return Ok(team);
    }
    
    /// <summary>
    /// Retrieves all teams within a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of teams within the organization.</returns>
    /// <example>GET /api/organization-teams/organization/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-teams/organization/550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    /// <response code="200">Returns the list of teams.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read teams.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("organization/{organizationId:guid}", Name = "GetOrganizationTeams")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationTeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewTeams)]
    public async Task<IActionResult> GetOrganizationTeams(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var teams = await teamService.GetOrganizationTeamsAsync(organizationId, cancellationToken);
        return Ok(teams);
    }
    
    
    /// <summary>
    /// Retrieves all members of a specific team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of team members.</returns>
    /// <example>GET /api/organization-teams/550e8400-e29b-41d4-a716-446655440000/members</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-teams/550e8400-e29b-41d4-a716-446655440000/members
    /// </remarks>
    /// <response code="200">Returns the list of team members.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read team members.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{teamId:guid}/members", Name = "GetTeamMembers")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationTeamMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewTeams)]
    public async Task<IActionResult> GetTeamMembers(Guid teamId, CancellationToken cancellationToken = default)
    {
        var members = await teamService.GetTeamMembersAsync(teamId, cancellationToken);
        return Ok(members);
    }
    
    
    /// <summary>
    /// Creates a new team within an organization.
    /// </summary>
    /// <param name="createTeamRequest">The team data to create.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created team details.</returns>
    /// <example>POST /api/organization-teams</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-teams
    ///      {
    ///         "organizationId": "550e8400-e29b-41d4-a716-446655440000",
    ///         "teamManagerId": "660f9511-f3ac-52e5-b827-557766551111",
    ///         "name": "Development Team",
    ///         "description": "Team responsible for software development"
    ///      }
    /// </remarks>
    /// <response code="201">Team created successfully.</response>
    /// <response code="400">Invalid request data or team manager is not a member of the organization.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to create teams.</response>
    /// <response code="409">Team name already exists in the organization.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost(Name = "CreateTeam")]
    [ProducesResponseType(typeof(OrganizationTeamDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.CreateTeam)]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest createTeamRequest, CancellationToken cancellationToken = default)
    {
        var team = await teamService.CreateTeamAsync(createTeamRequest);
        return CreatedAtRoute("GetTeamById", new { id = team.Id }, team);
    }
    
    /// <summary>
    /// Updates an existing team's name and/or description.
    /// </summary>
    /// <param name="id">The unique identifier of the team to update.</param>
    /// <param name="updateTeamDto">The updated team data.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the update operation.</returns>
    /// <example>PUT /api/organization-teams/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      PUT /api/organization-teams/550e8400-e29b-41d4-a716-446655440000
    ///      {
    ///         "name": "Updated Development Team",
    ///         "description": "Updated team description"
    ///      }
    /// </remarks>
    /// <response code="200">Team updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update teams.</response>
    /// <response code="404">Team with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPut("{id:guid}", Name = "UpdateTeam")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ManageTeamMembers)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamRequest updateTeamDto, CancellationToken cancellationToken = default)
    {
        var requestingUserId = GetCurrentUserId();
        var result = await teamService.UpdateTeamAsync(id, updateTeamDto, requestingUserId, cancellationToken);
        return Ok(result);
    }
    
    
    /// <summary>
    /// Deletes a team.
    /// </summary>
    /// <param name="id">The unique identifier of the team to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the deletion operation.</returns>
    /// <example>DELETE /api/organization-teams/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      DELETE /api/organization-teams/550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    /// <response code="204">Team deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to delete teams.</response>
    /// <response code="404">Team with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpDelete("{id:guid}", Name = "DeleteTeam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.DeleteTeam)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var requestingUserId = GetCurrentUserId();
        await teamService.DeleteTeamAsync(id, requestingUserId, cancellationToken);
        return NoContent();
    }
    
    
    /// <summary>
    /// Adds a member to a team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="addTeamMemberDto">The member data to add to the team.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the add operation.</returns>
    /// <example>POST /api/organization-teams/550e8400-e29b-41d4-a716-446655440000/members</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-teams/550e8400-e29b-41d4-a716-446655440000/members
    ///      {
    ///         "memberId": "660f9511-f3ac-52e5-b827-557766551111"
    ///      }
    /// </remarks>
    /// <response code="200">Member added to team successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to add team members.</response>
    /// <response code="409">Member is already a member of this team.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("{teamId:guid}/members", Name = "AddMemberToTeam")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ManageTeamMembers)]
    public async Task<IActionResult> AddMemberToTeam(Guid teamId, [FromBody] AddTeamMemberRequest addTeamMemberDto, CancellationToken cancellationToken = default)
    {
        var requestingUserId = GetCurrentUserId();
        var result = await teamService.AddMemberToTeamAsync(teamId, addTeamMemberDto.MemberId, requestingUserId, cancellationToken);
        return Ok(result);
    }
    
    /// <summary>
    /// Removes a member from a team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="memberId">The unique identifier of the member to remove.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the removal operation.</returns>
    /// <example>DELETE /api/organization-teams/550e8400-e29b-41d4-a716-446655440000/members/660f9511-f3ac-52e5-b827-557766551111</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      DELETE /api/organization-teams/550e8400-e29b-41d4-a716-446655440000/members/660f9511-f3ac-52e5-b827-557766551111
    /// </remarks>
    /// <response code="204">Member removed from team successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to remove team members.</response>
    /// <response code="404">Team member not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpDelete("{teamId:guid}/members/{memberId:guid}", Name = "RemoveMemberFromTeam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ManageTeamMembers)]
    public async Task<IActionResult> RemoveMemberFromTeam(Guid teamId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var requestingUserId = Guid.NewGuid();
        await teamService.RemoveMemberFromTeamAsync(teamId, memberId, requestingUserId, cancellationToken);
        return NoContent();
    }
    
    /// <summary>
    /// Changes the manager of a team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="changeManagerDto">The new manager data.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the manager change operation.</returns>
    /// <example>PUT /api/organization-teams/550e8400-e29b-41d4-a716-446655440000/manager</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      PUT /api/organization-teams/550e8400-e29b-41d4-a716-446655440000/manager
    ///      {
    ///         "newManagerId": "660f9511-f3ac-52e5-b827-557766551111"
    ///      }
    /// </remarks>
    /// <response code="200">Team manager changed successfully.</response>
    /// <response code="400">Invalid request data or new manager is not a member of the organization.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to change team manager.</response>
    /// <response code="404">Team not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPut("{teamId:guid}/manager", Name = "ChangeTeamManager")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ManageTeamMembers)]
    public async Task<IActionResult> ChangeTeamManager(Guid teamId, [FromBody] ChangeTeamManagerRequest changeManagerDto, CancellationToken cancellationToken = default)
    {
        var requestingUserId = Guid.NewGuid();
        var result = await teamService.ChangeTeamManagerAsync(teamId, changeManagerDto.NewManagerId, requestingUserId, cancellationToken);
        return Ok(result);
    }
}
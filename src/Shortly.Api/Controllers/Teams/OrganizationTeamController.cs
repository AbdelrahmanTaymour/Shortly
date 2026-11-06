using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Api.Extensions;
using Shortly.Api.Routing;
using Shortly.Core.Exceptions;
using Shortly.Domain.Abstractions;
using Shortly.Core.Teams.AddTeamMember;
using Shortly.Core.Teams.ChangeTeamManager;
using Shortly.Core.Teams.CheckTeamNameAvailability;
using Shortly.Core.Teams.CreateTeam;
using Shortly.Core.Teams.DeleteTeam;
using Shortly.Core.Teams.GetOrganizationTeams;
using Shortly.Core.Teams.GetTeam;
using Shortly.Core.Teams.GetTeamMembers;
using Shortly.Core.Teams.RemoveTeamMember;
using Shortly.Core.Teams.UpdateTeam;

namespace Shortly.Api.Controllers.Teams;

[ApiController]
[Authorize]
[Route("api/organizations/{organizationId:guid}/teams")]
[OrganizationRouteScope]
[Produces("application/json")]
public class OrganizationTeamsController(ISender sender) : ControllerBase
{
    /// <summary>Retrieves a specific team by its unique identifier.</summary>
    [HttpGet("{teamId:guid}", Name = "GetTeamById")]
    [ProducesResponseType(typeof(OrganizationTeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamById(Guid organizationId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTeamQuery(organizationId, teamId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
    
    /// <summary>Retrieves a paginated list of teams belonging to the organization.</summary>
    [HttpGet(Name = "GetOrganizationTeams")]
    [ProducesResponseType(typeof(PagedResponse<OrganizationTeamResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeams(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOrganizationTeamsQuery(organizationId, page, pageSize), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Retrieves a paginated list of members belonging to a specific team.</summary>
    [HttpGet("{teamId:guid}/members", Name = "GetTeamMembers")]
    [ProducesResponseType(typeof(PagedResponse<OrganizationTeamMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamMembers(Guid organizationId, Guid teamId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTeamMembersQuery(teamId, page, pageSize), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>
    /// Checks whether a team name is free in the organization.
    /// </summary>
    [HttpGet("name-available", Name = "CheckTeamNameAvailability")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckNameAvailability(
        Guid organizationId, [FromQuery] string name, [FromQuery] Guid? excludeTeamId = null, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CheckTeamNameAvailabilityCommand(organizationId, name, excludeTeamId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Creates a new team within the organization.</summary>
    [HttpPost(Name = "CreateTeam")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTeam(Guid organizationId, [FromBody] CreateTeamRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CreateTeamCommand(organizationId, request.TeamManagerId, request.Name, request.Description), cancellationToken);
        
        return result.IsFailure 
            ? result.ToProblemDetails() 
            : CreatedAtRoute("GetTeamById", new { organizationId, teamId = result.Value }, result.Value);
    }

    /// <summary>Updates an existing team's name or description.</summary>
    [HttpPatch("{teamId:guid}", Name = "UpdateTeam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeam(Guid organizationId, Guid teamId, [FromBody] UpdateTeamRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new UpdateTeamCommand(organizationId, teamId, request.Name, request.Description), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Permanently deletes a team from the organization.</summary>
    [HttpDelete("{teamId:guid}", Name = "DeleteTeam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeam(Guid organizationId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeleteTeamCommand(organizationId, teamId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Changes the designated manager for a team.</summary>
    [HttpPatch("{teamId:guid}/manager", Name = "ChangeTeamManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeManager(Guid organizationId, Guid teamId, [FromBody] ChangeTeamManagerRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ChangeTeamManagerCommand(organizationId, teamId, request.NewManagerId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Adds a member to a team.</summary>
    [HttpPost("{teamId:guid}/members", Name = "AddTeamMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddMember(Guid organizationId, Guid teamId, [FromBody] AddTeamMemberRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AddTeamMemberCommand(organizationId, teamId, request.MemberId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Removes a member from a team.</summary>
    [HttpDelete("{teamId:guid}/members/{memberId:guid}", Name = "RemoveTeamMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid organizationId, Guid teamId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RemoveTeamMemberCommand(organizationId, teamId, memberId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }
}
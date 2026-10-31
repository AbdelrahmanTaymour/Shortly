using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Exceptions.DTOs;
using Shortly.Core.Teams.AddTeamMember;
using Shortly.Core.Teams.ChangeTeamManager;
using Shortly.Core.Teams.CreateTeam;
using Shortly.Core.Teams.DeleteTeam;
using Shortly.Core.Teams.GetOrganizationTeams;
using Shortly.Core.Teams.GetTeam;
using Shortly.Core.Teams.GetTeamMembers;
using Shortly.Core.Teams.RemoveTeamMember;
using Shortly.Core.Teams.UpdateTeam;

namespace Shortly.API.Controllers.Teams;

[ApiController]
[Route("api/organizations/{organizationId:guid}/teams")]
[Produces("application/json")]
[Authorize]
public class OrganizationTeamsController(ISender sender) : ControllerBase
{
    [HttpGet("{teamId:guid}", Name = "GetTeamById")]
    [ProducesResponseType(typeof(OrganizationTeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamById(Guid organizationId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTeamQuery(organizationId, teamId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
    
    [HttpGet(Name = "GetOrganizationTeams")]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizationTeamResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeams(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOrganizationTeamsQuery(organizationId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("{teamId:guid}/members", Name = "GetTeamMembers")]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizationTeamMemberResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamMembers(Guid organizationId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTeamMembersQuery(teamId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost(Name = "CreateTeam")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTeam(Guid organizationId, [FromBody] CreateTeamRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CreateTeamCommand(organizationId, request.TeamManagerId, request.Name, request.Description), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : CreatedAtRoute("GetTeamById", new { organizationId, teamId = result.Value }, null);
    }

    [HttpPatch("{teamId:guid}", Name = "UpdateTeam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateTeam(Guid organizationId, Guid teamId, [FromBody] UpdateTeamRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new UpdateTeamCommand(organizationId, teamId, request.Name, request.Description), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpDelete("{teamId:guid}", Name = "DeleteTeam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTeam(Guid organizationId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeleteTeamCommand(organizationId, teamId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpPatch("{teamId:guid}/manager", Name = "ChangeTeamManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeManager(Guid organizationId, Guid teamId, [FromBody] ChangeTeamManagerRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ChangeTeamManagerCommand(organizationId, teamId, request.NewManagerId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpPost("{teamId:guid}/members", Name = "AddTeamMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddMember(Guid organizationId, Guid teamId, [FromBody] AddTeamMemberRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AddTeamMemberCommand(organizationId, teamId, request.MemberId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpDelete("{teamId:guid}/members/{memberId:guid}", Name = "RemoveTeamMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(Guid organizationId, Guid teamId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RemoveTeamMemberCommand(organizationId, teamId, memberId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }
}
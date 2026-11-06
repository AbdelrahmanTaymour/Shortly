using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Api.Extensions;
using Shortly.Api.Routing;
using Shortly.Core.Exceptions;
using Shortly.Core.Members.AddMember;
using Shortly.Core.Members.GetMembership;
using Shortly.Core.Members.GetOrganizationMembers;
using Shortly.Core.Members.LeaveOrganization;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Members.RemoveMember;
using Shortly.Core.Members.UpdateMemberRole;
using Shortly.Domain.Abstractions;

namespace Shortly.Api.Controllers.Members;

[ApiController]
[Authorize]
[Route("api/organizations/{organizationId:guid}/members")]
[OrganizationRouteScope]
[Produces("application/json")]
public class OrganizationMembersController(ISender sender) : ControllerBase
{
    /// <summary>Retrieves a paginated list of members belonging to the organization.</summary>
    [HttpGet(Name = "GetOrganizationMembers")]
    [ProducesResponseType(typeof(PagedResponse<OrganizationMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMembers(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOrganizationMembersQuery(organizationId, page, pageSize), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Leaves the organization. Returns tokens for the personal workspace; the owner must transfer ownership first.</summary>
    [HttpPost("leave", Name = "LeaveOrganization")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Leave(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new LeaveOrganizationCommand(organizationId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Retrieves membership details for a specific user within the organization.</summary>
    [HttpGet("{userId:guid}", Name = "GetMembership")]
    [ProducesResponseType(typeof(OrganizationMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembership(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMembershipQuery(organizationId, userId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Adds a new user as a member of the organization with a designated role.</summary>
    [HttpPost(Name = "AddMember")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddMember(Guid organizationId, [FromBody] AddMemberRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AddMemberCommand(organizationId, request.UserId, request.Role), cancellationToken);
        
        return result.IsFailure 
            ? result.ToProblemDetails() 
            : CreatedAtRoute("GetMembership", new { organizationId, userId = request.UserId }, null);
    }

    /// <summary>Removes a user from the organization.</summary>
    [HttpDelete("{userId:guid}", Name = "RemoveMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RemoveMemberCommand(organizationId, userId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Updates a member's role within the organization.</summary>
    [HttpPatch("{userId:guid}/role", Name = "UpdateMemberRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(Guid organizationId, Guid userId, [FromBody] UpdateMemberRoleRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new UpdateMemberRoleCommand(organizationId, userId, request.Role), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }
}
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Exceptions.DTOs;
using Shortly.Core.Members.AddMember;
using Shortly.Core.Members.GetMembership;
using Shortly.Core.Members.GetOrganizationMembers;
using Shortly.Core.Members.RemoveMember;
using Shortly.Core.Members.UpdateMemberRole;
using Shortly.Domain.Abstractions;

namespace Shortly.API.Controllers.Members;

[ApiController]
[Route("api/organizations/{organizationId:guid}/members")]
[Produces("application/json")]
[Authorize]
public class OrganizationMembersController(ISender sender) : ControllerBase
{
    [HttpGet(Name = "GetOrganizationMembers")]
    [ProducesResponseType(typeof(PagedResponse<OrganizationMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMembers(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOrganizationMembersQuery(organizationId, page, pageSize), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("{userId:guid}", Name = "GetMembership")]
    [ProducesResponseType(typeof(OrganizationMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembership(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMembershipQuery(organizationId, userId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost(Name = "AddMember")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddMember(Guid organizationId, [FromBody] AddMemberRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AddMemberCommand(organizationId, request.UserId, request.Role), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : CreatedAtRoute("GetMembership", new { organizationId, userId = request.UserId }, null);
    }

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
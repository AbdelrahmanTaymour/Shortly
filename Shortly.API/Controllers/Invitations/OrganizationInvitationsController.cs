using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Invitations.CancelInvitation;
using Shortly.Core.Invitations.CreateInvitation;
using Shortly.Core.Invitations.GetInvitationById;
using Shortly.Core.Invitations.GetOrganizationInvitations;
using Shortly.Core.Invitations.ResendInvitation;
using Shortly.Domain.Abstractions;

namespace Shortly.API.Controllers.Invitations;

[ApiController]
[Route("api/organizations/{organizationId:guid}/invitations")]
[Produces("application/json")]
[Authorize]
public class OrganizationInvitationsController(ISender sender) : ControllerBase
{
    [HttpGet(Name = "GetOrganizationInvitations")]
    [ProducesResponseType(typeof(PagedResponse<OrganizationInvitationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvitations(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOrganizationInvitationsQuery(organizationId, page, pageSize), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("{invitationId:guid}", Name = "GetInvitationById")]
    [ProducesResponseType(typeof(OrganizationInvitationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid organizationId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInvitationByIdQuery(invitationId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost(Name = "CreateInvitation")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid organizationId, [FromBody] CreateInvitationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CreateInvitationCommand(organizationId, request.Email, request.Role, request.CustomPermissions), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : CreatedAtRoute("GetInvitationById", new { organizationId, invitationId = result.Value }, null);
    }

    [HttpDelete("{invitationId:guid}", Name = "CancelInvitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid organizationId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CancelInvitationCommand(organizationId, invitationId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpPost("{invitationId:guid}/resend", Name = "ResendInvitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Resend(Guid organizationId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ResendInvitationCommand(organizationId, invitationId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }
}
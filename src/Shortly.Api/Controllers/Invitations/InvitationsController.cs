using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Invitations.AcceptInvitation;
using Shortly.Core.Invitations.RejectInvitation;
using Shortly.Core.Invitations.ValidateInvitationToken;

namespace Shortly.API.Controllers.Invitations;

[ApiController]
[Route("api/invitations")]
[Produces("application/json")]
public class InvitationsController(ISender sender) : ControllerBase
{
    [HttpGet("validate", Name = "ValidateInvitationToken")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> Validate([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ValidateInvitationTokenQuery(token), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("accept", Name = "AcceptInvitation")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Accept([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AcceptInvitationCommand(token), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpPost("reject", Name = "RejectInvitation")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RejectInvitationCommand(token), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }
}
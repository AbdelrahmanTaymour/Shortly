using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Api.Extensions;
using Shortly.Core.Exceptions;
using Shortly.Core.Invitations.AcceptInvitation;
using Shortly.Core.Invitations.RejectInvitation;
using Shortly.Core.Invitations.ValidateInvitationToken;

namespace Shortly.Api.Controllers.Invitations;

[ApiController]
[Route("api/invitations")]
[Produces("application/json")]
public class InvitationsController(ISender sender) : ControllerBase
{
    /// <summary>Validates whether an invitation token is still active and valid.</summary>
    [HttpGet("validate", Name = "ValidateInvitationToken")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validate([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ValidateInvitationTokenQuery(token), cancellationToken);
        
        // Return problem details if the token validation fails outright, otherwise return the boolean result
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Accepts an organization invitation, adding the authenticated user as a member.</summary>
    [HttpPost("accept", Name = "AcceptInvitation")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Accept([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AcceptInvitationCommand(token), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Rejects an organization invitation using the secure token.</summary>
    [HttpPost("reject", Name = "RejectInvitation")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RejectInvitationCommand(token), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }
}
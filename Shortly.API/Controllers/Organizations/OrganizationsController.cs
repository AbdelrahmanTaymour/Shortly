using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Exceptions.DTOs;
using Shortly.Core.Organizations.CreateOrganization;
using Shortly.Core.Organizations.ExitOrganization;
using Shortly.Core.Organizations.GetMyOrganizations;
using Shortly.Core.Organizations.SwitchOrganization;

namespace Shortly.API.Controllers.Organizations;

[ApiController]
[Route("api/organizations")]
[Produces("application/json")]
[Authorize]
public class OrganizationsController(ISender sender) : ControllerBase
{
    [HttpPost(Name = "CreateOrganization")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateOrganizationCommand(request.Name);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    [HttpPost("{id:guid}/switch", Name = "SwitchOrganization")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Switch(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new SwitchOrganizationCommand(id);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    [HttpPost("exit", Name = "ExitOrganization")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Exit(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ExitOrganizationCommand(), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    [HttpGet("mine", Name = "GetMyOrganizations")]
    [ProducesResponseType(typeof(IReadOnlyList<MyOrganizationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyOrganizationsQuery(), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }
}
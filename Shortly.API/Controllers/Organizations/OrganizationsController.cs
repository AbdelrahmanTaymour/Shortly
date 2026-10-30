using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Exceptions.DTOs;
using Shortly.Core.Organizations.ActivateOrganization;
using Shortly.Core.Organizations.CreateOrganization;
using Shortly.Core.Organizations.DeactivateOrganization;
using Shortly.Core.Organizations.DeleteOrganization;
using Shortly.Core.Organizations.ExitOrganization;
using Shortly.Core.Organizations.GetMyOrganizations;
using Shortly.Core.Organizations.GetOrganizationUsage;
using Shortly.Core.Organizations.SwitchOrganization;
using Shortly.Core.Organizations.TransferOwnership;
using Shortly.Core.Organizations.UpdateOrganization;

namespace Shortly.API.Controllers.Organizations;

[ApiController]
[Route("api/organizations")]
[Produces("application/json")]
[Authorize]
public class OrganizationsController(ISender sender) : ControllerBase
{
    [HttpGet("mine", Name = "GetMyOrganizations")]
    [ProducesResponseType(typeof(IReadOnlyList<MyOrganizationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyOrganizationsQuery(), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
    
    [HttpPost(Name = "CreateOrganization")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateOrganizationCommand(request.Name, request.Description, request.Website, request.LogoUrl, request.MemberLimit);
        var result = await sender.Send(command, cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPut("{id:guid}", Name = "UpdateOrganization")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateOrganizationCommand(id, request.Name, request.Description, request.Website, request.LogoUrl, request.MemberLimit);
        var result = await sender.Send(command, cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpDelete("{id:guid}", Name = "DeleteOrganization")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeleteOrganizationCommand(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }
    
    [HttpPost("{id:guid}/activate", Name = "AdminActivateOrganization")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ActivateOrganizationCommand(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok();
    }

    [HttpPost("{id:guid}/deactivate", Name = "AdminDeactivateOrganization")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeactivateOrganizationCommand(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok();
    }

    [HttpPost("{id:guid}/transfer-ownership", Name = "TransferOrganizationOwnership")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferOwnership(Guid id, [FromBody] TransferOwnershipRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new TransferOwnershipCommand(id, request.NewOwnerId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpPost("{id:guid}/switch", Name = "SwitchOrganization")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Switch(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SwitchOrganizationCommand(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost("exit", Name = "ExitOrganization")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Exit(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ExitOrganizationCommand(), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("{id:guid}/usage", Name = "GetOrganizationUsage")]
    [ProducesResponseType(typeof(OrganizationUsageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsage(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOrganizationUsageQuery(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
}
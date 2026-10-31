using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Admin.GetOrganizationDetail;
using Shortly.Core.Admin.ResetAllOrganizationsUsage;
using Shortly.Core.Admin.ResetOrganizationUsage;
using Shortly.Core.Admin.SearchOrganizations;
using Shortly.Core.Exceptions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;
using Shortly.Infrastructure.Authorization;

namespace Shortly.API.Controllers.Admin;

[ApiController]
[Route("api/admin/organizations")]
[Produces("application/json")]
[Authorize]
public class AdminOrganizationsController(ISender sender) : ControllerBase
{
    [HttpGet("search", Name = "AdminSearchOrganizations")]
    [RequiredPermission(enPermissions.SystemAdmin)]
    [ProducesResponseType(typeof(PagedResponse<OrganizationSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchOrganizationsQuery(search, page, pageSize), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("{id:guid}", Name = "AdminGetOrganizationDetail")]
    [RequiredPermission(enPermissions.SystemAdmin)]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOrganizationDetailQuery(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost("{id:guid}/usage/reset", Name = "AdminResetOrganizationUsage")]
    [RequiredPermission(enPermissions.ManageOrgUsage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetUsage(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ResetOrganizationUsageCommand(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok();
    }

    [HttpPost("usage/reset-all", Name = "AdminResetAllOrganizationsUsage")]
    [RequiredPermission(enPermissions.ManageOrgUsage)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetAllUsage(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ResetAllOrganizationsUsageCommand(), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(new { organizationsAffected = result.Value });
    }
}
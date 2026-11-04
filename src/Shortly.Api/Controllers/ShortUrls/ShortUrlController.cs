using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shortly.Api.Extensions;
using Shortly.Api.Filters;
using Shortly.Api.Routing;
using Shortly.Core.Exceptions;
using Shortly.Core.ShortUrls.ActivateShortUrl;
using Shortly.Core.ShortUrls.AssignShortCode;
using Shortly.Core.ShortUrls.BulkActivateShortUrls;
using Shortly.Core.ShortUrls.BulkCreateShortUrls;
using Shortly.Core.ShortUrls.BulkDeactivateShortUrls;
using Shortly.Core.ShortUrls.BulkDeleteShortUrls;
using Shortly.Core.ShortUrls.BulkUpdateExpiration;
using Shortly.Core.ShortUrls.CheckShortCodeAvailability;
using Shortly.Core.ShortUrls.CreateShortUrl;
using Shortly.Core.ShortUrls.DeactivateShortUrl;
using Shortly.Core.ShortUrls.DeleteShortUrl;
using Shortly.Core.ShortUrls.GetMyShortUrls;
using Shortly.Core.ShortUrls.GetShortUrlByCode;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Core.ShortUrls.RemoveShortUrlPassword;
using Shortly.Core.ShortUrls.SearchShortUrls;
using Shortly.Core.ShortUrls.SetShortUrlPassword;
using Shortly.Core.ShortUrls.UpdateShortUrl;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;
using Shortly.Infrastructure.EdgeProtection.Models;

namespace Shortly.Api.Controllers.ShortUrls;

[ApiController]
[Authorize]
[Route("api/short-urls")]
[OrganizationScopedRoutes("short-urls")]
[OrganizationRouteScope]
[Produces("application/json")]
public class ShortUrlController(ISender sender) : ControllerBase
{
    /// <summary>Retrieves a short URL by its numeric ID, falling back to a short code lookup if needed.</summary>
    [HttpGet("{id:long}", Name = "GetShortUrlById")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetShortUrlByIdQuery(id), cancellationToken);

        // If the ID lookup fails, check if the route text represents an all-digit custom short code (e.g., "2024001")
        if (result.IsFailure && result.Error == ShortUrlErrors.NotFound &&
            Request.RouteValues["id"]?.ToString() is { Length: > 0 } routeText)
        {
            var byCode = await sender.Send(new GetShortUrlByCodeQuery(routeText), cancellationToken);
            if (byCode.IsSuccess) return Ok(byCode.Value);
        }

        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
    
    /// <summary>Retrieves a short URL directly by its short code identifier.</summary>
    [HttpGet("{shortCode}", Name = "GetShortUrlByShortCode")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShortUrlByShortCode(string shortCode, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetShortUrlByCodeQuery(shortCode), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Retrieves a paginated list of short URLs belonging to the current user.</summary>
    [HttpGet("mine", Name = "GetMyShortUrls")]
    [ProducesResponseType(typeof(PagedResponse<ShortUrlResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyShortUrlsQuery(page, pageSize), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Searches and filters short URLs based on query parameters.</summary>
    [HttpGet("search", Name = "SearchShortUrls")]
    [ProducesResponseType(typeof(PagedResponse<ShortUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] SearchShortUrlsQuery query, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(query, cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Creates a new short URL, allowing anonymous creation protected by rate limiting.</summary>
    [HttpPost(Name = "CreateShortUrl")]
    [AllowAnonymous]
    [RejectInvalidBearerToken]
    [EnableRateLimiting(AnonymousShortUrlCreationRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateShortUrlCommand command, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : CreatedAtAction(nameof(GetShortUrlByShortCode), new { shortCode = result.Value }, result.Value);
    }

    /// <summary>Updates an existing short URL's properties.</summary>
    [HttpPut("{id:long}", Name = "UpdateShortUrl")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateShortUrlRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateShortUrlCommand(
            id, request.OriginalUrl, request.ShortCode, request.Title, request.Description,
            request.ClickLimit, request.IsActive, request.IsPrivate, request.TrackingEnabled, request.ExpiresAt);

        var result = await sender.Send(command, cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }
    
    /// <summary>Assigns or updates a custom short code for an existing link.</summary>
    [HttpPatch("{id:long}/short-code", Name = "AssignShortCode")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignShortCode(long id, [FromBody] AssignShortCodeRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AssignShortCodeCommand(id, request.NewShortCode), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Checks if a specific short code is available for use.</summary>
    [HttpGet("code-available", Name = "CheckShortCodeAvailability")]
    [EnableRateLimiting(ShortCodeAvailabilityRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckCodeAvailability([FromQuery] string shortCode, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CheckShortCodeAvailabilityCommand(shortCode), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Activates a deactivated short URL.</summary>
    [HttpPost("{id:long}/activate", Name = "ActivateShortUrl")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(long id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ActivateShortUrlCommand(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Deactivates an active short URL, preventing redirection.</summary>
    [HttpPost("{id:long}/deactivate", Name = "DeactivateShortUrl")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeactivateShortUrlCommand(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Sets a password requirement on a short URL.</summary>
    [HttpPost("{id:long}/password", Name = "SetShortUrlPassword")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPassword(long id, [FromBody] SetShortUrlPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SetShortUrlPasswordCommand(id, request.Password), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Removes the password protection from a short URL.</summary>
    [HttpDelete("{id:long}/password", Name = "RemoveShortUrlPassword")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePassword(long id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RemoveShortUrlPasswordCommand(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    /// <summary>Permanently deletes a short URL.</summary>
    [HttpDelete("{id:long}", Name = "DeleteShortUrl")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeleteShortUrlCommand(id), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }
    
    /// <summary>Creates multiple short URLs in a single batch operation.</summary>
    [HttpPost("bulk", Name = "BulkCreateShortUrls")]
    [ProducesResponseType(typeof(BulkCreateShortUrlsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateShortUrlsCommand command, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Updates expiration dates for multiple short URLs in bulk.</summary>
    [HttpPatch("bulk/expiration", Name = "BulkUpdateExpiration")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUpdateExpiration([FromBody] BulkUpdateExpirationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkUpdateExpirationCommand(request.ShortUrlIds, request.ExpiresAt), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Activates multiple short URLs in a single batch operation.</summary>
    [HttpPost("bulk/activate", Name = "BulkActivateShortUrls")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkActivate([FromBody] BulkShortUrlIdsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkActivateShortUrlsCommand(request.ShortUrlIds), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Deactivates multiple short URLs in a single batch operation.</summary>
    [HttpPost("bulk/deactivate", Name = "BulkDeactivateShortUrls")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkDeactivate([FromBody] BulkShortUrlIdsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkDeactivateShortUrlsCommand(request.ShortUrlIds), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    /// <summary>Deletes multiple short URLs in a single batch operation.</summary>
    [HttpDelete("bulk", Name = "BulkDeleteShortUrls")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkDelete([FromBody] BulkShortUrlIdsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkDeleteShortUrlsCommand(request.ShortUrlIds), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
}
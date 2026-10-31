using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
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

namespace Shortly.API.Controllers.ShortUrls;

[ApiController]
[Authorize]
[Route("api/short-urls")]
[Produces("application/json")]
public class ShortUrlController(ISender sender) : ControllerBase
{
    [HttpGet("{id:long}", Name = "GetShortUrlById")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetShortUrlByIdQuery(id), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }
    
    [HttpGet("{shortCode}", Name = "GetShortUrlByShortCode")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShortUrlByShortCode(string shortCode, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetShortUrlByCodeQuery(shortCode), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("mine", Name = "GetMyShortUrls")]
    [ProducesResponseType(typeof(PagedResponse<ShortUrlResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyShortUrlsQuery(page, pageSize), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    [HttpGet("search", Name = "SearchShortUrls")]
    [ProducesResponseType(typeof(PagedResponse<ShortUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] SearchShortUrlsRequest request, CancellationToken cancellationToken = default)
    {
        var query = new SearchShortUrlsQuery(
            request.Search, request.Status, request.Visibility, request.DateFrom, request.DateTo,
            request.SortBy, request.Page, request.PageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    [HttpPost(Name = "CreateShortUrl")]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateShortUrlCommand command, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : CreatedAtAction(nameof(GetShortUrlByShortCode), new { shortCode = result.Value }, result.Value);
    }

    [HttpPut("{id:long}", Name = "UpdateShortUrl")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateShortUrlRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateShortUrlCommand(
            id, request.OriginalUrl, request.ShortCode, request.Title, request.Description,
            request.ClickLimit, request.IsActive, request.IsPrivate, request.TrackingEnabled, request.ExpiresAt);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Ok();
    }
    
    [HttpPatch("{id:long}/short-code", Name = "AssignShortCode")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignShortCode(long id, [FromBody] AssignShortCodeRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AssignShortCodeCommand(id, request.NewShortCode), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok();
    }

    [HttpGet("code-available", Name = "CheckShortCodeAvailability")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckCodeAvailability([FromQuery] string shortCode, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CheckShortCodeAvailabilityQuery(shortCode), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost("{id:long}/activate", Name = "ActivateShortUrl")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(long id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ActivateShortUrlCommand(id), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok();
    }

    [HttpPost("{id:long}/deactivate", Name = "DeactivateShortUrl")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeactivateShortUrlCommand(id), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok();
    }

    [HttpPost("{id:long}/password", Name = "SetShortUrlPassword")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPassword(long id, [FromBody] SetShortUrlPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SetShortUrlPasswordCommand(id, request.Password), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok();
    }

    [HttpDelete("{id:long}/password", Name = "RemoveShortUrlPassword")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePassword(long id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RemoveShortUrlPasswordCommand(id), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok();
    }

    [HttpDelete("{id:long}", Name = "DeleteShortUrl")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeleteShortUrlCommand(id), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : NoContent();
    }
    
    [HttpPost("bulk", Name = "BulkCreateShortUrls")]
    [Authorize]
    [ProducesResponseType(typeof(BulkCreateShortUrlsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateShortUrlsCommand command, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPatch("bulk/expiration", Name = "BulkUpdateExpiration")]
    [Authorize]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUpdateExpiration([FromBody] BulkUpdateExpirationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkUpdateExpirationCommand(request.ShortUrlIds, request.ExpiresAt), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost("bulk/activate", Name = "BulkActivateShortUrls")]
    [Authorize]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkActivate([FromBody] BulkShortUrlIdsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkActivateShortUrlsCommand(request.ShortUrlIds), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    [HttpPost("bulk/deactivate", Name = "BulkDeactivateShortUrls")]
    [Authorize]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkDeactivate([FromBody] BulkShortUrlIdsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkDeactivateShortUrlsCommand(request.ShortUrlIds), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    [HttpDelete("bulk", Name = "BulkDeleteShortUrls")]
    [Authorize]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkDelete([FromBody] BulkShortUrlIdsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkDeleteShortUrlsCommand(request.ShortUrlIds), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }
}
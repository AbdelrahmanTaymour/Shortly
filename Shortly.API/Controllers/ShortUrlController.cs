using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.ServiceContracts;
using Swashbuckle.AspNetCore.Annotations;

namespace Shortly.API.Controllers;

/// <summary>
///     Controller for managing short URLs.
///     Provides endpoints for creating, retrieving, updating, and deleting shortened URLs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Short URL Management", Description = "Endpoints for creating and managing shortened URLs")]
public class ShortUrlController(IShortUrlsService shortUrlsService) : ControllerBase
{
    private readonly IShortUrlsService _shortUrlsService = shortUrlsService;

    /// <summary>
    ///     Retrieves a paginated list of all short URLs stored in the system.
    /// </summary>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>A paginated list of short URLs with metadata.</returns>
    /// <response code="200">Successfully retrieved the list of short URLs.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet("getAll", Name = "GetAll")]
    [ProducesResponseType(typeof(List<ShortUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get all short URLs",
        Description = "Retrieves a paginated list of all short URLs with optional filtering and sorting.",
        OperationId = "GetAllShortUrls"
    )]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var shortUrls = await _shortUrlsService.GetAllAsync();
        
        // Apply pagination
        var totalItems = shortUrls.Count;
        var paginatedUrls = shortUrls
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(paginatedUrls);
    }
    
    /// <summary>
    ///     Redirects to the original URL using a short code.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to redirect to.</param>
    /// <returns>HTTP redirect to the original URL.</returns>
    /// <response code="301">Permanent redirect to the original URL.</response>
    /// <response code="404">Short code not found.</response>
    /// <response code="500">Internal server error occurred.</response>
    /// <remarks>
    ///     Using RedirectPermanent (301) causes the browser to cache the redirect,
    ///     so future requests skip the server entirely and go straight to the target URL.
    ///     This results in the access counter increasing only once per browser.
    /// </remarks>
    [AllowAnonymous]
    [HttpGet("{shortCode}", Name = "GetByShortCode")]
    [ProducesResponseType(StatusCodes.Status301MovedPermanently)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Redirect to original URL",
        Description = "Redirects to the original URL using the provided short code.",
        OperationId = "RedirectToOriginalUrl"
    )]
    public async Task<IActionResult> GetByShortCode(string shortCode)
    {
        var shortUrlResponse = await _shortUrlsService.GetByShortCodeAsync(shortCode);
        if (shortUrlResponse is null)
        {
            return NotFound();
        }

        return RedirectPermanent(shortUrlResponse.OriginalUrl);
    }
    
    /// <summary>
    ///     Creates a new shortened URL.
    /// </summary>
    /// <param name="shortUrlRequest">The request containing the original URL to be shortened.</param>
    /// <returns>The newly created short URL with details.</returns>
    /// <response code="201">Short URL created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="409">URL already exists.</response>
    /// <response code="500">Internal server error occurred.</response>
    [AllowAnonymous]
    [HttpPost(Name = "CreateShortUrl")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Create short URL",
        Description = "Creates a new shortened URL from the provided original URL.",
        OperationId = "CreateShortUrl"
    )]
    public async Task<IActionResult> CreateShortUrl([FromBody] ShortUrlRequest shortUrlRequest)
    {
        var shortUrlResponse = await _shortUrlsService.CreateShortUrlAsync(shortUrlRequest);
        
        return CreatedAtAction(
            nameof(GetByShortCode), 
            new { shortCode = shortUrlResponse.ShortCode }, 
            shortUrlResponse);
    }
    
    /// <summary>
    ///     Updates an existing short URL by its short code.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to update.</param>
    /// <param name="updatedShortUrl">The updated URL information.</param>
    /// <returns>The updated short URL details.</returns>
    /// <response code="200">Short URL updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Short code not found.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpPut("{shortCode:required}", Name = "UpdateShortUrlByShortCode")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Update short URL",
        Description = "Updates an existing short URL with new information.",
        OperationId = "UpdateShortUrl"
    )]
    public async Task<IActionResult> UpdateShortUrlByShortCode(string shortCode, [FromBody] ShortUrlRequest updatedShortUrl)
    {
        var shortUrlResponse = await _shortUrlsService.UpdateShortUrlAsync(shortCode, updatedShortUrl);
        if (shortUrlResponse == null)
        {
            return NotFound();
        }

        return Ok(shortUrlResponse);
    }
    
    /// <summary>
    ///     Deletes an existing short URL by its short code.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to delete.</param>
    /// <returns>No content if successful.</returns>
    /// <response code="204">Short URL deleted successfully.</response>
    /// <response code="404">Short code not found.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpDelete("{shortCode}", Name = "DeleteShortUrlByShortCode")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Delete short URL",
        Description = "Deletes an existing short URL by its short code.",
        OperationId = "DeleteShortUrl"
    )]
    public async Task<IActionResult> DeleteShortUrlByShortCode(string shortCode)
    {
        bool isDeleted = await _shortUrlsService.DeleteShortUrlAsync(shortCode);
        if (!isDeleted)
        {
            return NotFound();
        }

        return NoContent();
    }
    
    /// <summary>
    ///     Retrieves statistics for a short URL.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to retrieve statistics for.</param>
    /// <returns>Statistics for the short URL including click counts and analytics.</returns>
    /// <response code="200">Statistics retrieved successfully.</response>
    /// <response code="404">Short code not found.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet("{shortCode}/stats", Name = "GetStatistics")]
    [ProducesResponseType(typeof(StatusShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get URL statistics",
        Description = "Retrieves detailed statistics and analytics for a specific short URL.",
        OperationId = "GetUrlStatistics"
    )]
    public async Task<IActionResult> GetStatistics(string shortCode)
    {
        var shortUrlStatistics = await _shortUrlsService.GetStatisticsAsync(shortCode);
        if (shortUrlStatistics is null)
        {
            return NotFound();
        }

        return Ok(shortUrlStatistics);
    }
}
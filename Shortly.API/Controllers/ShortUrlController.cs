using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs;
using Shortly.Core.ServiceContracts;
using Shortly.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Shortly.API.Controllers;

/// <summary>
///     Controller for managing short URLs with comprehensive performance monitoring and caching.
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
    [ProducesResponseType(typeof(ApiResponseDto<List<ShortUrlResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get all short URLs",
        Description = "Retrieves a paginated list of all short URLs with optional filtering and sorting.",
        OperationId = "GetAllShortUrls"
    )]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = HttpContext.TraceIdentifier;

        try
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

            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
            var pagination = new PaginationInfo(page, pageSize, totalItems);

            var response = ApiResponseDto<List<ShortUrlResponse>>.SuccessWithPagination(
                paginatedUrls,
                pagination,
                "Short URLs retrieved successfully",
                traceId,
                performance);

            return Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
            
            var errorResponse = ApiResponseDto<List<ShortUrlResponse>>.Error(
                "Failed to retrieve short URLs",
                ex.Message,
                traceId,
                performance);

            return StatusCode(500, errorResponse);
        }
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
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Redirect to original URL",
        Description = "Redirects to the original URL using the provided short code.",
        OperationId = "RedirectToOriginalUrl"
    )]
    public async Task<IActionResult> GetByShortCode(string shortCode)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var shortUrlResponse = await _shortUrlsService.GetByShortCodeAsync(shortCode);
            if (shortUrlResponse is null)
            {
                stopwatch.Stop();
                var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
                
                var errorResponse = ApiResponseDto<object>.Error(
                    "Short code not found",
                    new { ShortCode = shortCode },
                    traceId,
                    performance);

                return NotFound(errorResponse);
            }

            stopwatch.Stop();
            
            // Add performance headers for monitoring
            Response.Headers.Append("X-Response-Time", $"{stopwatch.ElapsedMilliseconds}ms");
            Response.Headers.Append("X-Trace-Id", traceId);

            return RedirectPermanent(shortUrlResponse.OriginalUrl);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
            
            var errorResponse = ApiResponseDto<object>.Error(
                "Failed to process redirect",
                ex.Message,
                traceId,
                performance);

            return StatusCode(500, errorResponse);
        }
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
    [ProducesResponseType(typeof(ApiResponseDto<ShortUrlResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Create short URL",
        Description = "Creates a new shortened URL from the provided original URL.",
        OperationId = "CreateShortUrl"
    )]
    public async Task<IActionResult> CreateShortUrl([FromBody] ShortUrlRequest shortUrlRequest)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var shortUrlResponse = await _shortUrlsService.CreateShortUrlAsync(shortUrlRequest);
            
            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);

            var response = ApiResponseDto<ShortUrlResponse>.Success(
                shortUrlResponse,
                "Short URL created successfully",
                traceId,
                performance);

            return CreatedAtAction(
                nameof(GetByShortCode), 
                new { shortCode = shortUrlResponse.ShortCode }, 
                response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
            
            var errorResponse = ApiResponseDto<ShortUrlResponse>.Error(
                "Failed to create short URL",
                ex.Message,
                traceId,
                performance);

            return StatusCode(500, errorResponse);
        }
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
    [ProducesResponseType(typeof(ApiResponseDto<ShortUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Update short URL",
        Description = "Updates an existing short URL with new information.",
        OperationId = "UpdateShortUrl"
    )]
    public async Task<IActionResult> UpdateShortUrlByShortCode(string shortCode, [FromBody] ShortUrlRequest updatedShortUrl)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var shortUrlResponse = await _shortUrlsService.UpdateShortUrlAsync(shortCode, updatedShortUrl);
            if (shortUrlResponse == null)
            {
                stopwatch.Stop();
                var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
                
                var errorResponse = ApiResponseDto<ShortUrlResponse>.Error(
                    "Short code not found",
                    new { ShortCode = shortCode },
                    traceId,
                    performance);

                return NotFound(errorResponse);
            }

            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);

            var response = ApiResponseDto<ShortUrlResponse>.Success(
                shortUrlResponse,
                "Short URL updated successfully",
                traceId,
                performance);

            return Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
            
            var errorResponse = ApiResponseDto<ShortUrlResponse>.Error(
                "Failed to update short URL",
                ex.Message,
                traceId,
                performance);

            return StatusCode(500, errorResponse);
        }
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
    [ProducesResponseType(typeof(ApiResponseDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Delete short URL",
        Description = "Deletes an existing short URL by its short code.",
        OperationId = "DeleteShortUrl"
    )]
    public async Task<IActionResult> DeleteShortUrlByShortCode(string shortCode)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            bool isDeleted = await _shortUrlsService.DeleteShortUrlAsync(shortCode);
            if (!isDeleted)
            {
                stopwatch.Stop();
                var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
                
                var errorResponse = ApiResponseDto.Error(
                    "Short code not found",
                    new { ShortCode = shortCode },
                    traceId,
                    performance);

                return NotFound(errorResponse);
            }

            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);

            var response = ApiResponseDto.Success(
                "Short URL deleted successfully",
                traceId,
                performance);

            return NoContent();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
            
            var errorResponse = ApiResponseDto.Error(
                "Failed to delete short URL",
                ex.Message,
                traceId,
                performance);

            return StatusCode(500, errorResponse);
        }
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
    [ProducesResponseType(typeof(ApiResponseDto<StatusShortUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get URL statistics",
        Description = "Retrieves detailed statistics and analytics for a specific short URL.",
        OperationId = "GetUrlStatistics"
    )]
    public async Task<IActionResult> GetStatistics(string shortCode)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var shortUrlStatistics = await _shortUrlsService.GetStatisticsAsync(shortCode);
            if (shortUrlStatistics is null)
            {
                stopwatch.Stop();
                var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
                
                var errorResponse = ApiResponseDto<StatusShortUrlResponse>.Error(
                    "Short code not found",
                    new { ShortCode = shortCode },
                    traceId,
                    performance);

                return NotFound(errorResponse);
            }

            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);

            var response = ApiResponseDto<StatusShortUrlResponse>.Success(
                shortUrlStatistics,
                "Statistics retrieved successfully",
                traceId,
                performance);

            return Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var performance = new PerformanceMetrics(stopwatch.ElapsedMilliseconds);
            
            var errorResponse = ApiResponseDto<StatusShortUrlResponse>.Error(
                "Failed to retrieve statistics",
                ex.Message,
                traceId,
                performance);

            return StatusCode(500, errorResponse);
        }
    }
}
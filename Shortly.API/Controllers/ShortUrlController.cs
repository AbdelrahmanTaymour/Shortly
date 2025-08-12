using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Core.ServiceContracts.UrlManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;


/// <summary>
/// Controller for managing short URLs, providing CRUD operations and URL shortening functionality.
/// </summary>
/// <remarks>
/// This controller handles the creation, retrieval, updating, and deletion of short URLs.
/// </remarks>
[ApiController]
[Route("api/short-urls")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class ShortUrlController(IShortUrlsService shortUrlsService, IAuthenticationContextProvider contextProvider) : ControllerApiBase
{

    /// <summary>
    /// Retrieves a short URL by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The short URL details if found.</returns>
    /// <example>GET /api/short-urls/123456789</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/short-urls/123456789
    /// </remarks>
    /// <response code="200">Returns the short URL details.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read URLs.</response>
    /// <response code="404">Short URL with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{id:long:required}", Name = "GetShortUrlById")]
    [ProducesResponseType(typeof(ShortUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ReadUrl)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var shortUrlDto = await shortUrlsService.GetByIdAsync(id, includeTracking: false, cancellationToken);
        return Ok(shortUrlDto);
    }
    
    
    /// <summary>
    /// Retrieves a short URL by its short code.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The short URL details if found.</returns>
    /// <example>GET /api/short-urls/code/my-short-code</example>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/short-urls/code/my-short-code
    /// </remarks>
    /// <response code="200">Returns the short URL details.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read URLs.</response>
    /// <response code="404">Short URL with the specified short code was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("code/{shortCode}", Name = "GetShortUrlByCode")]
    [ProducesResponseType(typeof(ShortUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ReadUrl)]
    public async Task<IActionResult> GetByShortCode(string shortCode, CancellationToken cancellationToken = default)
    {
        var shortUrlDto = await shortUrlsService.GetByShortCodeAsync(shortCode, includeTracking: false, cancellationToken);
        return Ok(shortUrlDto);
    }

    
    /// <summary>
    /// Creates a new short URL.
    /// </summary>
    /// <param name="request">The request containing the URL details to create.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created short URL details.</returns>
    /// <example>
    /// POST /api/short-urls
    /// </example>
    /// <remarks>
    /// <para><strong>Access:</strong> [AllowAnonymous] This endpoint is accessible to everyone (anonymous clients, logged-in users, and organization members). 
    /// The service will handle permissions and feature availability based on the user's authentication status and role.</para>
    /// Sample request:
    /// 
    ///     POST /api/short-urls
    ///     {
    ///         "originalUrl": "https://www.example.com",
    ///         "customShortCode": "my-custom-code",
    ///         "clickLimit": 100,
    ///         "trackingEnabled": true,
    ///         "isPasswordProtected": false,
    ///         "isPrivate": false,
    ///         "expiresAt": "2024-12-31T23:59:59Z",
    ///         "title": "Example Website",
    ///         "description": "This is an example website"
    ///     }
    /// 
    /// </remarks>
    /// <response code="201">Short URL was created successfully.</response>
    /// <response code="400">The request data is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to create URLs.</response>
    /// <response code="409">A short URL with the specified custom code already exists.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost(Name = "CreateShortUrl")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> CreateNewShortUrl([FromBody] CreateShortUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await shortUrlsService.AddAsync(request, HttpContext, cancellationToken);
        return CreatedAtAction("GetById", new {id = response.Id}, response);
    }
    
    
    /// <summary>
    /// Updates an existing short URL by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL to update.</param>
    /// <param name="request">The request containing the updated URL details.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The updated short URL details.</returns>
    /// <example>
    /// PUT /api/short-urls/123456789
    /// </example>
    /// <remarks>
    /// Sample request (all fields are optional for updates):
    /// 
    ///     PUT /api/short-urls/123456789
    ///     {
    ///         "originalUrl": "https://www.updated-example.com",
    ///         "isActive": true,
    ///         "trackingEnabled": false,
    ///         "clickLimit": 50,
    ///         "title": "Updated Title"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Short URL was updated successfully.</response>
    /// <response code="400">The request data is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update URLs.</response>
    /// <response code="404">Short URL with the specified ID was not found.</response>
    /// <response code="409">Update would create a conflict (e.g., duplicate short code).</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPut("{id:long}", Name = "UpdateShortUrl")]
    [ProducesResponseType(typeof(ShortUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.UpdateUrl)]
    public async Task<IActionResult> UpdateById(long id, [FromBody] UpdateShortUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        var shortUrlDto = await shortUrlsService.UpdateByIdAsync(id, request, cancellationToken);
        return Ok(shortUrlDto);
    }
    
    
    /// <summary>
    /// Updates the short code of an existing short URL.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL.</param>
    /// <param name="newShortCode">The new short code to assign to the URL.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the operation.</returns>
    /// <remarks>
    /// This endpoint allows you to change only the short code of an existing URL.
    /// </remarks>
    /// <response code="200">Short code was updated successfully.</response>
    /// <response code="400">The new short code is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update URLs.</response>
    /// <response code="404">Short URL with the specified ID was not found.</response>
    /// <response code="409">A short URL with the new short code already exists.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPatch("{id:long}/short-code", Name = "UpdateShortCode")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.UpdateUrl)]
    public async Task<IActionResult> UpdateShortCode(long id, [FromBody] string newShortCode, CancellationToken cancellationToken = default)
    {
        var succeed = await shortUrlsService.UpdateShortCodeAsync(id, newShortCode, cancellationToken);
        return Ok(succeed);
    }
    
    
    /// <summary>
    /// Deletes a short URL by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the short URL to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">Short URL was deleted successfully.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to delete URLs.</response>
    /// <response code="404">Short URL with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpDelete("{id:long:required}", Name = "DeleteShortUrlById")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.DeleteUrl)]
    public async Task<IActionResult> DeleteShortUrl(long id, CancellationToken cancellationToken = default)
    {
        await shortUrlsService.DeleteByIdAsync(id, cancellationToken);
        return NoContent();
    }
    
    
    /// <summary>
    /// Deletes a short URL by its short code.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">Short URL was deleted successfully.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to delete URLs.</response>
    /// <response code="404">Short URL with the specified short code was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpDelete("code/{shortCode}", Name = "DeleteShortUrlByCode")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.DeleteUrl)]
    public async Task<IActionResult> DeleteShortUrlByCode(string shortCode, CancellationToken cancellationToken = default)
    {
        await shortUrlsService.DeleteByShortCodeAsync(shortCode, cancellationToken);
        return NoContent();
    }

     
    /// <summary>
    /// Checks whether the shortCode is existing
    /// </summary>
    /// <param name="shortCode">The shortCode to check if exists.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// <c>true</c> if shortCode exists; otherwise <c>false</c>
    /// </returns>
    /// <response code="200">Returns true if shortCode exists; otherwise false.</response>
    /// <response code="400">The provided shortCode is invalid.</response>
    /// <response code="500">An internal server error occurred during the check.</response>
    /// <remarks>
    /// This endpoint allows anonymous access to facilitate URL creation processes.
    /// Returns true when the shortCode is already taken.
    /// Sample Request:
    ///
    ///     GET /api/short-urls/code-exists?shortCode=xUfMQv
    /// </remarks>
    /// <example>
    ///     GET /api/short-urls/code-exists?shortCode=xUfMQv
    /// </example>
    [HttpGet("code-exists", Name = "IsShortCodeExists")]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> IsShortCodeExists([FromQuery] string shortCode,
        CancellationToken cancellationToken = default)
    {
        var result = await shortUrlsService.IsShortCodeExistsAsync(shortCode, cancellationToken);
        return Ok(result);
    }
    
}
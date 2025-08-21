using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Core.ServiceContracts.UrlManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/short-urls/bulk")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class ShortUrlBulkOperationsController(IUrlBulkOperationsService bulkOperationsService, IAuthenticationContextProvider contextProvider) : ControllerApiBase
{
    /// <summary>
    /// Creates multiple short URLs in a single batch operation.
    /// </summary>
    /// <param name="request">A collection of short URL creation requests to process in bulk.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A detailed result containing success/failure counts and any conflict information.</returns>
    /// <example>POST /api/short-urls/bulk/create</example>
    /// <remarks>
    /// <para><strong>Access:</strong> The service will handle permissions and feature availability based on the user's authentication status and role.
    /// </para>
    /// Sample request:
    /// 
    ///     POST /api/short-urls/bulk/create
    ///     {
    ///       "requests": [
    ///         {
    ///           "originalUrl": "https://www.example1.com",
    ///           "customShortCode": "example1",
    ///           "clickLimit": 100,
    ///           "trackingEnabled": true,
    ///           "title": "Example 1"
    ///         },
    ///         {
    ///           "originalUrl": "https://www.example2.com",
    ///           "customShortCode": "example2",
    ///           "clickLimit": 100,
    ///           "trackingEnabled": true,
    ///           "title": "Example 2"
    ///         }
    ///     }
    /// 
    /// <para>
    /// <strong>Performance Benefits:</strong>
    /// - Reduced network overhead compared to individual requests
    /// - Optimized database operations with batch processing
    /// - Transactional consistency across multiple URL creations
    /// <strong>Conflict Handling:</strong>
    /// - URLs with conflicting custom codes are skipped and reported
    /// - Successful URLs are still created even if some fail
    /// - Detailed conflict messages help identify and resolve issues</para>
    /// </remarks>
    /// <response code="200">Bulk creation completed. Check response for detailed results including any conflicts.</response>
    /// <response code="400">The request data is invalid or malformed (e.g., empty request collection, invalid URL formats).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to create URLs in bulk.</response>
    /// <response code="500">An internal server error occurred during bulk processing.</response>
    [HttpPost("create", Name = "BulkCreateShortUrls")]
    [ProducesResponseType(typeof(BulkCreateShortUrlResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.BulkCreateLinks)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateShortUrlsRequest request, CancellationToken cancellationToken = default)
    {
        var authContext = contextProvider.GetCurrentContextAsync(HttpContext);
        var result = await bulkOperationsService.BulkCreateAsync(request.Requests, authContext, cancellationToken);
        return Ok(result);
    }
    
    
    /// <summary>
    /// Updates the expiration date for multiple short URLs in a single batch operation.
    /// </summary>
    /// <param name="request">The bulk update request containing URL IDs and the new expiration date.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A result containing the count of successfully updated URLs and any failures.</returns>
    /// <example>PUT /api/short-urls/bulk/update-expiration</example>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/short-urls/bulk/update-expiration
    ///     {
    ///         "ids": [123456789, 987654321, 456789123],
    ///         "newExpirationDate": "2024-12-31T23:59:59Z"
    ///     }
    /// 
    /// This endpoint allows efficient management of URL expiration dates across multiple URLs.
    /// Setting newExpirationDate to null removes the expiration (makes URLs permanent).
    /// 
    /// **Use Cases:**
    /// - Extending expiration for campaign URLs
    /// - Batch management of temporary URLs
    /// - Emergency expiration updates
    /// - Removing expiration from URLs that should be permanent
    /// 
    /// Only URLs that the authenticated user has permission to modify will be updated.
    /// </remarks>
    /// <response code="200">Bulk expiration update completed. Check response for detailed results.</response>
    /// <response code="400">The request data is invalid (e.g., empty ID collection, invalid date format).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update URLs in bulk.</response>
    /// <response code="500">An internal server error occurred during bulk processing.</response>
    [HttpPut("update-expiration", Name = "BulkUpdateExpiration")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.BulkUpdateLinks)]
    public async Task<IActionResult> BulkUpdateExpiration([FromBody] BulkUpdateExpirationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await bulkOperationsService.BulkUpdateExpirationAsync(request.Ids, request.NewExpirationDate, cancellationToken);
        return Ok(result);
    }
    
    
    /// <summary>
    /// Deletes multiple short URLs in a single batch operation.
    /// </summary>
    /// <param name="request">The bulk delete request containing the IDs of URLs to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A result containing the count of successfully deleted URLs and any failures.</returns>
    /// <example>DELETE /api/short-urls/bulk/delete</example>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /api/short-urls/bulk/delete
    ///     {
    ///         "ids": [123456789, 987654321, 456789123]
    ///     }
    /// 
    /// **⚠️ WARNING: This operation permanently deletes URLs and cannot be undone.**
    /// 
    /// This endpoint efficiently removes multiple URLs in a single operation.
    /// All associated analytics data and click history will also be deleted.
    /// 
    /// **Use Cases:**
    /// - Cleanup of expired or obsolete URLs
    /// - Bulk removal of test URLs
    /// - Administrative maintenance operations
    /// - User account cleanup
    /// 
    /// Only URLs that the authenticated user has permission to delete will be removed.
    /// </remarks>
    /// <response code="200">Bulk deletion completed. Check response for detailed results.</response>
    /// <response code="400">The request data is invalid (e.g., empty ID collection).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to delete URLs in bulk.</response>
    /// <response code="500">An internal server error occurred during bulk processing.</response>
    [HttpDelete("delete", Name = "BulkDeleteShortUrls")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.BulkDeleteLinks)]
    public async Task<IActionResult> BulkDelete([FromBody] BasicBulkRequest request, CancellationToken cancellationToken = default)
    {
       var result = await bulkOperationsService.BulkDeleteAsync(request.Ids, cancellationToken);
        return Ok(result);
    }
    
    
    /// <summary>
    /// Deletes all expired short URLs from the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A result containing the count of successfully deleted expired URLs.</returns>
    /// <example>DELETE /api/short-urls/bulk/delete-expired</example>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /api/short-urls/bulk/delete-expired
    /// 
    /// **⚠️ WARNING: This operation permanently deletes all expired URLs and cannot be undone.**
    /// 
    /// This endpoint performs system-wide cleanup by removing URLs that have passed their expiration date.
    /// It's typically used for:
    /// - Automated maintenance jobs
    /// - Database cleanup operations
    /// - Storage optimization
    /// - Compliance with data retention policies
    /// 
    /// **Administrative Operation:**
    /// This is a system-level operation that affects URLs across all users and organizations.
    /// Only users with appropriate administrative permissions should have access to this endpoint.
    /// </remarks>
    /// <response code="200">Expired URLs cleanup completed. Check response for the number of deleted URLs.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to perform system-wide cleanup operations.</response>
    /// <response code="500">An internal server error occurred during cleanup processing.</response>
    [HttpDelete("delete-expired", Name = "DeleteExpiredShortUrls")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.CleanupSystemData)]
    public async Task<IActionResult> DeleteExpired(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var result = await bulkOperationsService.DeleteExpiredAsync(nowUtc, cancellationToken);
        return Ok(result);
    }


    /// <summary>
    /// Activates multiple short URLs in a single batch operation.
    /// </summary>
    /// <param name="request">The bulk activation request containing the IDs of URLs to activate.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A result containing the count of successfully activated URLs and any failures.</returns>
    /// <example>PUT /api/short-urls/bulk/activate</example>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/short-urls/bulk/activate
    ///     {
    ///         "ids": [123456789, 987654321, 456789123]
    ///     }
    /// 
    /// This endpoint enables multiple URLs that were previously deactivated.
    /// Activated URLs become accessible again and can be used for redirections.
    /// 
    /// **Use Cases:**
    /// - Re-enabling URLs after maintenance
    /// - Bulk activation of seasonal campaigns
    /// - Recovery from accidental deactivation
    /// - Scheduled activation of prepared URLs
    /// 
    /// Only URLs that the authenticated user has permission to modify will be activated.
    /// URLs that are already active will be included in the success count without any changes.
    /// </remarks>
    /// <response code="200">Bulk activation completed. Check response for detailed results.</response>
    /// <response code="400">The request data is invalid (e.g., empty ID collection).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to activate URLs in bulk.</response>
    /// <response code="500">An internal server error occurred during bulk processing.</response>
    [HttpPut("activate", Name = "BulkActivateShortUrls")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.BulkUpdateLinks)]
    public async Task<IActionResult> BulkActivate([FromBody] BasicBulkRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await bulkOperationsService.BulkActivateAsync(request.Ids, cancellationToken);
        return Ok(result);
    }
    
    /// <summary>
    /// Deactivate multiple short URLs in a single batch operation.
    /// </summary>
    /// <param name="request">The bulk deactivated request containing the IDs of URLs to deactivate.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A result containing the count of successfully deactivated URLs and any failures.</returns>
    /// <example>PUT /api/short-urls/bulk/deactivate</example>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/short-urls/bulk/deactivate
    ///     {
    ///         "ids": [123456789, 987654321, 456789123]
    ///     }
    /// 
    /// This endpoint enables multiple URLs that were previously deactivated.
    /// Deactivate URLs become accessible again and can be used for redirections.
    /// 
    /// **Use Cases:**
    /// - Re-enabling URLs after maintenance
    /// - Bulk deactivation of seasonal campaigns
    /// 
    /// Only URLs that the authenticated user has permission to modify will be deactivated.
    /// URLs that are already not active will be included in the success count without any changes.
    /// </remarks>
    /// <response code="200">Bulk deactivation completed. Check response for detailed results.</response>
    /// <response code="400">The request data is invalid (e.g., empty ID collection).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to deactivate URLs in bulk.</response>
    /// <response code="500">An internal server error occurred during bulk processing.</response>
    [HttpPut("deactivate", Name = "BulkDeactivateShortUrls")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.BulkUpdateLinks)]
    public async Task<IActionResult> BulkDeactivate([FromBody] BasicBulkRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await bulkOperationsService.BulkDeactivateAsync(request.Ids, cancellationToken);
        return Ok(result);
    }

}
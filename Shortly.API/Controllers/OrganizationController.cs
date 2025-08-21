using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

/// <summary>
/// Controller for managing organizations, providing CRUD operations and organization management functionality.
/// </summary>
/// <remarks>
/// This controller handles the creation, retrieval, updating, deletion, and management of organizations
/// including ownership transfer, activation/deactivation, and member access validation.
/// </remarks>
[ApiController]
[Route("api/organizations")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class OrganizationController(IOrganizationService organizationService) : ControllerApiBase
{
    /// <summary>
    /// Retrieves all organizations with pagination support.
    /// </summary>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 50, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of organizations.</returns>
    /// <example>GET /api/organizations?page=1&amp;pageSize=10</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organizations?page=1&amp;pageSize=10
    /// </remarks>
    /// <response code="200">Returns the list of organizations.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read organizations.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet(Name = "GetAllOrganizations")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewOrganization)]
    public async Task<IActionResult> GetAll(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var organizations = await organizationService.GetAllAsync(page, pageSize, cancellationToken);
        return Ok(organizations);
    }
    
    /// <summary>
    /// Retrieves an organization by its unique identifier.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The organization details if found.</returns>
    /// <example>GET /api/organizations/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organizations/550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    /// <response code="200">Returns the organization details.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read organizations.</response>
    /// <response code="404">Organization with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{organizationId:guid}", Name = "GetOrganizationById")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewOrganization)]
    public async Task<IActionResult> GetById(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var organization = await organizationService.GetOrganizationAsync(organizationId, cancellationToken);
        return Ok(organization);
    }
    
    /// <summary>
    /// Searches organizations by name with pagination support.
    /// </summary>
    /// <param name="searchTerm">The search term to filter organizations by name.</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 50, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of organizations matching the search criteria.</returns>
    /// <example>GET /api/organizations/search?searchTerm=tech&amp;page=1&amp;pageSize=10</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organizations/search?searchTerm=tech&amp;page=1&amp;pageSize=10
    /// </remarks>
    /// <response code="200">Returns the list of organizations matching the search criteria.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read organizations.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("search", Name = "SearchOrganizations")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewOrganization)]
    public async Task<IActionResult> Search([FromQuery] string searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var organizations = await organizationService.SearchOrganizationsAsync(searchTerm, page, pageSize, cancellationToken);
        return Ok(organizations);
    }
    
    /// <summary>
    /// Retrieves all organizations owned by a specific user.
    /// </summary>
    /// <param name="ownerId">The unique identifier of the owner.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of organizations owned by the user.</returns>
    /// <example>GET /api/organizations/user/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organizations/user/550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    /// <response code="200">Returns the list of user's organizations.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read organizations.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("user/{ownerId:guid}", Name = "GetUserOrganizations")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewOrganization)]
    public async Task<IActionResult> GetUserOrganizations(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var organizations = await organizationService.GetUserOrganizationsAsync(ownerId, cancellationToken);
        return Ok(organizations);
    }
    
    /// <summary>
    /// Creates a new organization.
    /// </summary>
    /// <param name="createOrganizationDto">The organization data to create.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created organization details.</returns>
    /// <example>POST /api/organizations</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organizations
    ///      {
    ///         "name": "Tech Corp",
    ///         "description": "A technology company",
    ///         "website": "https://techcorp.com",
    ///         "logoUrl": "https://techcorp.com/logo.png",
    ///         "memberLimit": 100,
    ///         "ownerId": "550e8400-e29b-41d4-a716-446655440000"
    ///      }
    /// </remarks>
    /// <response code="201">Organization created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to create organizations.</response>
    /// <response code="404">Owner user not found.</response>
    /// <response code="409">Organization with the same name already exists for the same owner.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost(Name = "CreateOrganization")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationDto createOrganizationDto, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var organization = await organizationService.CreateOrganizationAsync(createOrganizationDto, currentUserId);
        return CreatedAtRoute("GetOrganizationById", new { id = organization.Id }, organization);
    }
    
    /// <summary>
    /// Updates an existing organization.
    /// </summary>
    /// <param name="id">The unique identifier of the organization to update.</param>
    /// <param name="updateOrganizationDto">The updated organization data.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The updated organization details.</returns>
    /// <example>PUT /api/organizations/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      PUT /api/organizations/550e8400-e29b-41d4-a716-446655440000
    ///      {
    ///         "name": "Updated Tech Corp",
    ///         "description": "An updated technology company",
    ///         "website": "https://updatedtechcorp.com",
    ///         "logoUrl": "https://updatedtechcorp.com/logo.png",
    ///         "memberLimit": 150,
    ///         "isActive": true,
    ///         "isSubscribed": true
    ///      }
    /// </remarks>
    /// <response code="200">Organization updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update organizations.</response>
    /// <response code="404">Organization with the specified ID was not found.</response>
    /// <response code="409">The Owner is already the owner of this organization's updated name.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPut("{id:guid}", Name = "UpdateOrganization")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.UpdateOrganization)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationDto updateOrganizationDto, CancellationToken cancellationToken = default)
    {
        var organization = await organizationService.UpdateOrganizationAsync(id, updateOrganizationDto, cancellationToken);
        return Ok(organization);
    }
    
    /// <summary>
    /// Deletes an organization. Only the owner can delete the organization.
    /// </summary>
    /// <param name="id">The unique identifier of the organization to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the deletion operation.</returns>
    /// <example>DELETE /api/organizations/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      DELETE /api/organizations/550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    /// <response code="204">Organization deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to delete the organization or is not the owner.</response>
    /// <response code="404">Organization with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpDelete("{id:guid}", Name = "DeleteOrganization")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.DeleteOrganization)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        await organizationService.DeleteOrganizationAsync(id, currentUserId, cancellationToken);
        return NoContent();
    }
    
    
    /// <summary>
    /// Activates an organization.
    /// </summary>
    /// <param name="id">The unique identifier of the organization to activate.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the activation operation.</returns>
    /// <example>POST /api/organizations/550e8400-e29b-41d4-a716-446655440000/activate</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organizations/550e8400-e29b-41d4-a716-446655440000/activate
    /// </remarks>
    /// <response code="200">Organization activated successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to activate organizations.</response>
    /// <response code="404">Organization with the specified ID was not found or already activated.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPut("{id:guid}/activate", Name = "ActivateOrganization")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ManageOrganization)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await organizationService.ActivateOrganizationAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deactivates an organization.
    /// </summary>
    /// <param name="id">The unique identifier of the organization to deactivate.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the deactivation operation.</returns>
    /// <example>POST /api/organizations/550e8400-e29b-41d4-a716-446655440000/deactivate</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organizations/550e8400-e29b-41d4-a716-446655440000/deactivate
    /// </remarks>
    /// <response code="200">Organization deactivated successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to deactivate organizations.</response>
    /// <response code="404">Organization with the specified ID was not found or already deactivated.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPut("{id:guid}/deactivate", Name = "DeactivateOrganization")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ManageOrganization)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await organizationService.DeactivateOrganizationAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Transfers ownership of an organization from the current owner to a new owner.
    /// </summary>
    /// <param name="id">The unique identifier of the organization.</param>
    /// <param name="request">The transfer ownership data containing the new owner ID.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the ownership transfer operation.</returns>
    /// <example>POST /api/organizations/550e8400-e29b-41d4-a716-446655440000/transfer-ownership</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organizations/550e8400-e29b-41d4-a716-446655440000/transfer-ownership
    ///      {
    ///         "newOwnerId": "660f9511-f3ac-52e5-b827-557766551111"
    ///      }
    /// </remarks>
    /// <response code="200">Ownership transferred successfully.</response>
    /// <response code="400">Invalid request data or new owner is not a member of the organization.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to transfer ownership or is not the current owner.</response>
    /// <response code="404">Organization with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("{id:guid}/transfer-ownership", Name = "TransferOrgOwnership")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.TransferOrgOwnership)]
    public async Task<IActionResult> TransferOwnership(Guid id, [FromBody] TransferOwnershipRequest request, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var result = await organizationService.TransferOwnershipAsync(id, currentUserId, request.NewOwnerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Checks if a user has access to an organization (i.e., is a member).
    /// </summary>
    /// <param name="id">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user to check access for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the user has access to the organization, false otherwise.</returns>
    /// <example>GET /api/organizations/550e8400-e29b-41d4-a716-446655440000/access/660f9511-f3ac-52e5-b827-557766551111</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organizations/550e8400-e29b-41d4-a716-446655440000/access/660f9511-f3ac-52e5-b827-557766551111
    /// </remarks>
    /// <response code="200">Returns the access status.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to check organization access.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{id:guid}/access/{userId:guid}", Name = "CheckUserOrganizationAccess")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewOrganization)]
    public async Task<IActionResult> CheckUserAccess(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await organizationService.CanUserAccessOrganizationAsync(userId, id, cancellationToken);
        return Ok(hasAccess);
    }
}
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;

namespace Shortly.Core.ServiceContracts.OrganizationManagement;

/// <summary>
///     Defines services for managing organization-level usage statistics, including tracking link and QR code creation,
///     monitoring usage limits, and handling monthly usage resets.
/// </summary>
public interface IOrganizationUsageService
{
    /// <summary>
    ///     Retrieves comprehensive usage statistics for a specific organization.
    ///     This method is functionally equivalent to <see cref="GetByOrganizationIdAsync" /> but provides semantic clarity for usage
    ///     reporting scenarios.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="OrganizationUsageDto" /> containing the organization's comprehensive usage statistics.</returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified organization ID.</exception>
    Task<OrganizationUsageDto> GetUsageStatsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Tracks the creation of a new link by incrementing the organization's monthly link creation count.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization that owns the link.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    ///     <c>true</c> if the link creation was successfully tracked; otherwise, <c>false</c> if the organization has
    ///     exceeded its limit.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified organization ID.</exception>
    /// <exception cref="DatabaseException">Thrown if a database operation fails unexpectedly.</exception>
    Task<bool> TrackLinkCreationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Tracks the creation of a new QR code by incrementing the organization's monthly QR code creation count.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization that owns the QR code.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    ///     <c>true</c> if the QR code creation was successfully tracked; otherwise, <c>false</c> if the organization has
    ///     exceeded its limit.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified organization ID.</exception>
    /// <exception cref="DatabaseException">Thrown if a database operation fails unexpectedly.</exception>
    Task<bool> TrackQrCodeCreationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Determines whether an organization can create more links based on their current usage and subscription plan limits.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    ///     <c>true</c> if the organization can create more links; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified organization ID.</exception>
    /// <exception cref="DatabaseException">Thrown if a database operation fails unexpectedly.</exception>
    Task<bool> CanCreateMoreLinksAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Determines whether an organization can create more QR codes based on their current usage and subscription plan limits.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the organization can create more QR codes; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified organization ID.</exception>
    /// <exception cref="DatabaseException">Thrown if a database operation fails unexpectedly.</exception>
    Task<bool> CanCreateMoreQrCodesAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Resets the monthly usage statistics (links and QR codes created) for a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization whose usage should be reset.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    ///     <c>true</c> if the organization's monthly usage was successfully reset; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified organization ID.</exception>
    /// <exception cref="DatabaseException">Thrown if a database operation fails unexpectedly.</exception>
    /// <remarks>
    ///     Resets the usage counters to zero and updates the reset date to the next billing cycle.
    /// </remarks>
    Task<bool> ResetMonthlyUsageAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets monthly usage statistics (links and QR codes created) for all non-deleted organizations whose reset date has passed.
    ///     This method is typically used in scheduled maintenance or batch operations to handle monthly billing cycles.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if at least one organization usage record was reset; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     This operation logs information about the number of organizations affected and the reset date.
    ///     A warning is logged if no organizations matched the reset criteria.
    /// </remarks>
    Task<int> ResetMonthlyUsageForAllAsync(CancellationToken cancellationToken = default);

}

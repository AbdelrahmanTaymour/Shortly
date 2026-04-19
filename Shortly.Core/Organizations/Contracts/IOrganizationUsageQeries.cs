using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Organizations.DTOs;

namespace Shortly.Core.Organizations.Contracts;

public interface IOrganizationUsageQeries
{
    /// <summary>
    ///     Retrieves the subscription limits and status information for a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to retrieve limits for.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     An <see cref="OrganizationLimits"/> object containing the organization's subscription limits and status, 
    ///     or <c>null</c> if the organization is not found.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown if the database operation fails unexpectedly.</exception>
    /// <remarks>
    ///     This method uses a read-only query with minimal data projection for optimal performance. 
    ///     The returned limits include subscription plan constraints and organization status flags.
    /// </remarks>
    Task<OrganizationLimits?> GetOrganizationLimitsAsync(Guid organizationId, CancellationToken cancellationToken = default);

}
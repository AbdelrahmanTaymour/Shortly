using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Organizations.Contracts;
using Shortly.Core.Organizations.DTOs;
using Shortly.Core.Organizations.Mappers;
using Shortly.Domain.RepositoryContract.Organizations;

namespace Shortly.Core.Organizations.Services;

/// <summary>
/// Provides services for managing organization-level usage statistics.
/// Allows tracking resources such as link and QR code creation while ensuring concurrency safety.
/// </summary>
public class OrganizationUsageService(
    IOrganizationUsageRepository usageRepository,
    IOrganizationUsageQeries usageQeries,
    ILogger<OrganizationUsageService> logger) 
    : IOrganizationUsageService
{
    /// <inheritdoc/>
    public async Task<OrganizationUsageDto> GetUsageStatsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var orgUsage = await usageRepository.GetUsageStatsAsync(organizationId, cancellationToken) ??
                       throw new NotFoundException("OrganizationUsage", organizationId);
        return orgUsage.MapToOrganizationUsageDto();
    }

    /// <inheritdoc/>
    public async Task<bool> TrackLinkCreationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await usageRepository.IncrementLinksCreatedAsync(organizationId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TrackQrCodeCreationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await usageRepository.IncrementQrCodesCreatedAsync(organizationId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CanCreateMoreLinksAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var orgLimits = await usageQeries.GetOrganizationLimitsAsync(organizationId, cancellationToken);
        if (orgLimits == null || orgLimits.IsDeleted || !orgLimits.IsActive)
            return false;

        if (orgLimits.MaxLinksPerMonth < 0) // Unlimited
            return true;
        
        return await usageRepository.CanCreateMoreLinksAsync(organizationId, orgLimits.MaxLinksPerMonth, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CanCreateMoreQrCodesAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var orgLimits = await usageQeries.GetOrganizationLimitsAsync(organizationId, cancellationToken);
        if (orgLimits == null || orgLimits.IsDeleted || !orgLimits.IsActive)
            return false;

        if (orgLimits.MaxQrCodesPerMonth < 0) // Unlimited
            return true;
        
        return await usageRepository.CanCreateMoreQrCodesAsync(organizationId, orgLimits.MaxQrCodesPerMonth, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ResetMonthlyUsageAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await usageRepository.ResetMonthlyUsageAsync(organizationId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> ResetMonthlyUsageForAllAsync(CancellationToken cancellationToken = default)
    {
        return await usageRepository.BulkResetMonthlyUsageAsync(cancellationToken);
    }
}
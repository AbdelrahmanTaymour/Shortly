using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Models;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.OrganizationManagement;

/// <summary>
/// SQL Server repository for managing organization usage statistics.
/// Implements reliable concurrency-safe increments for usage tracking.
/// </summary>
public class OrganizationUsageRepository(SQLServerDbContext dbContext, ILogger<OrganizationUsageRepository> logger)
    : IOrganizationUsageRepository
{
    /// <inheritdoc/>
    public async Task<OrganizationUsage?> GetUsageStatsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationUsage
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.OrganizationId == organizationId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve usage stats for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("Error retrieving organization usage data.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<OrganizationUsage?> CreateOrganizationUsageAsync(OrganizationUsage entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.OrganizationUsage.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to creating usage for organization {OrganizationId}.", entity.OrganizationId);
            throw new DatabaseException("Error creating organization usage data.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IncrementLinksCreatedAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationUsage
                .Where(ou => ou.OrganizationId == organizationId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(ou => ou.MonthlyLinksCreated, ou => ou.MonthlyLinksCreated + 1)
                        .SetProperty(ou => ou.TotalLinksCreated, ou => ou.TotalLinksCreated + 1)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing links for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("Error incrementing links.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IncrementQrCodesCreatedAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationUsage
                .Where(ou => ou.OrganizationId == organizationId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(ou => ou.MonthlyQrCodesCreated, ou => ou.MonthlyQrCodesCreated + 1)
                        .SetProperty(ou => ou.TotalQrCodesCreated, ou => ou.TotalQrCodesCreated + 1)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing QR codes for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("Error incrementing QR codes.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<OrganizationLimits?> GetOrganizationLimitsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.Id == organizationId)
                .Select(o => new OrganizationLimits
                {
                    IsActive = o.IsActive,
                    IsDeleted = o.DeletedAt != null,
                    MaxLinksPerMonth = o.SubscriptionPlan.MaxLinksPerMonth,
                    MaxQrCodesPerMonth = o.SubscriptionPlan.MaxQrCodesPerMonth
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting organization limits for {OrganizationId}", organizationId);
            throw new DatabaseException("Error retrieving organization limits.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CanCreateMoreLinksAsync(Guid organizationId, int maxLinksPerMonth, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationUsage
                .AnyAsync(o => o.OrganizationId == organizationId && o.MonthlyLinksCreated < maxLinksPerMonth, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Error checking if organization {OrganizationId} can create more links", organizationId);
            throw new DatabaseException("Error checking organization can create more links.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CanCreateMoreQrCodesAsync(Guid organizationId, int maxQrCodesPerMonth,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationUsage
                .AnyAsync(o => o.OrganizationId == organizationId && o.MonthlyQrCodesCreated < maxQrCodesPerMonth, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Error checking if organization {OrganizationId} can create more QrCodes", organizationId);
            throw new DatabaseException("Error checking organization can create more QrCodes.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ResetMonthlyUsageAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationUsage
                .Where(ou => ou.OrganizationId == organizationId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(ou => ou.MonthlyLinksCreated, 0)
                        .SetProperty(ou => ou.MonthlyQrCodesCreated, 0)
                        .SetProperty(ou => ou.MonthlyResetDate, DateTime.UtcNow.AddMonths(1))
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting monthly usage for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("Error resetting monthly usage.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<int> BulkResetMonthlyUsageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationUsage
                .AsNoTracking()
                .Where(ou => ou.MonthlyResetDate < DateTime.UtcNow)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(ou => ou.MonthlyLinksCreated, 0)
                        .SetProperty(ou => ou.MonthlyQrCodesCreated, 0)
                        .SetProperty(ou => ou.MonthlyResetDate, DateTime.UtcNow.AddMonths(1))
                    , cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while resetting organizations monthly usage.");
            throw new DatabaseException("Failed to reset organizations monthly usage.", ex);
        }
    }
}
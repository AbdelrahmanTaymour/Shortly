using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Configuration;

namespace Shortly.Core.Services.UserManagement;

/// <summary>
///     Provides services for managing user usage statistics, including tracking link and QR code creation,
///     monitoring usage limits, and handling monthly usage resets.
/// </summary>
public class UserUsageService(IUserUsageRepository usageRepository, ILogger<UserUsageService> logger)
    : IUserUsageService
{
    /// <inheritdoc/>
    public async Task<UserUsageDto> GetUsageStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userUsage = await usageRepository.GetByUserIdAsync(userId, cancellationToken)
                        ?? throw new NotFoundException("UserUsage", userId);
        return userUsage.MapToUserUsageDto();
    }

    /// <inheritdoc/>
    public async Task<bool> TrackLinkCreationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await usageRepository.IncrementLinksCreatedAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TrackQrCodeCreationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await usageRepository.IncrementQrCodesCreatedAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CanCreateMoreLinksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var usage = await usageRepository.GetUserUsageWithPlanIdAsync(userId, cancellationToken)
                    ?? throw new NotFoundException("UserUsage", userId);

        var maxLinks = PlanConfiguration.Plans[usage.SubscriptionPlanId]
            .Limits[PlanConfiguration.enPlanLimits.UrlsPerMonth];
        return usage.MonthlyLinksCreated < maxLinks;
    }

    /// <inheritdoc/>
    public async Task<bool> CanCreateMoreQrCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var usage = await usageRepository.GetUserUsageWithPlanIdAsync(userId, cancellationToken)
                    ?? throw new NotFoundException("UserUsage", userId);

        var maxQrCodes = PlanConfiguration.Plans[usage.SubscriptionPlanId]
            .Limits[PlanConfiguration.enPlanLimits.QrCodesPerMonth];
        return usage.MonthlyQrCodesCreated < maxQrCodes;
    }

    /// <inheritdoc/>
    public async Task<int> GetRemainingLinksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var usage = await usageRepository.GetUserUsageWithPlanIdAsync(userId, cancellationToken)
                    ?? throw new NotFoundException("UserUsage", userId);

        var max = PlanConfiguration.Plans[usage.SubscriptionPlanId].Limits[PlanConfiguration.enPlanLimits.UrlsPerMonth];
        return Math.Max(max - usage.MonthlyLinksCreated, 0);
    }

    /// <inheritdoc/>
    public async Task<int> GetRemainingQrCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var usage = await usageRepository.GetUserUsageWithPlanIdAsync(userId, cancellationToken)
                    ?? throw new NotFoundException("UserUsage", userId);

        var max = PlanConfiguration.Plans[usage.SubscriptionPlanId].Limits[PlanConfiguration.enPlanLimits.QrCodesPerMonth];
        return Math.Max(max - usage.MonthlyQrCodesCreated, 0);
    }

    /// <inheritdoc/>
    public async Task<bool> HasExceededLimitsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var usage = await usageRepository.GetUserUsageWithPlanIdAsync(userId, cancellationToken)
                    ?? throw new NotFoundException("UserUsage", userId);

        var config = PlanConfiguration.Plans[usage.SubscriptionPlanId];
        return usage.MonthlyLinksCreated >= config.Limits[PlanConfiguration.enPlanLimits.UrlsPerMonth]
               || usage.MonthlyQrCodesCreated >= config.Limits[PlanConfiguration.enPlanLimits.QrCodesPerMonth];
    }

    /// <inheritdoc/>
    public async Task<bool> ResetMonthlyUsageAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await usageRepository.ResetMonthlyUsageAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ResetMonthlyUsageForAllAsync(CancellationToken cancellationToken = default)
    {
        var resetDate = DateTime.UtcNow;

        var affectedUsersCount = await usageRepository.ResetMonthlyUsageForAllAsync(resetDate, cancellationToken);

        if (affectedUsersCount > 0)
        {
            logger.LogInformation("{Count} user usage records were reset successfully on {ResetDate}.",
                affectedUsersCount, resetDate.Date);
            return true;
        }

        logger.LogWarning(
            "No user usage records were reset. Possibly no users matched the reset criteria as of {ResetDate}.",
            resetDate.Date);
        return false;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserUsageDto>> GetUsageReportAsync(DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        var users = await usageRepository.GetUsersWithResetDateInRangeAsync(from, to, cancellationToken);
        return users.Select(u => u.MapToUserUsageDto());
    }
}
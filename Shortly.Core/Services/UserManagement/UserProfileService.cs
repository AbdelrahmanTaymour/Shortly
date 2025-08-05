using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UsersDTOs.Profile;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Configuration;

namespace Shortly.Core.Services.UserManagement;

/// <summary>
/// Provides methods to manage user profiles and track their usage quotas.
/// </summary>
/// <param name="profileRepository">The repository for user profile data.</param>
/// <param name="userRepository">The repository for user accounts.</param>
/// <param name="usageRepository">The repository for tracking user usage statistics.</param>
/// <param name="logger">Logger instance for logging operations and exceptions.</param>
public class UserProfileService(IUserProfileRepository profileRepository, IUserRepository userRepository,
    IUserUsageRepository usageRepository, ILogger<UserProfileService> logger) : IUserProfileService
{
    
    /// <inheritdoc />
    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            throw new NotFoundException("User Profile", userId);
        }
        return profile.MapToUserProfile();
    }

    /// <inheritdoc />
    public async Task<bool> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            throw new NotFoundException("User Profile", userId);
        }
        
        profile.Name = request.Name;
        profile.Bio = request.Bio;
        profile.PhoneNumber = request.PhoneNumber;
        profile.ProfilePictureUrl = request.ProfilePictureUrl;
        profile.Website = request.Website;
        profile.Company = request.Company;
        profile.Location = request.Location;
        profile.Country = request.Country;
        profile.TimeZone = request.TimeZone;
        profile.UpdatedAt = DateTime.UtcNow;
        
        var updated = await profileRepository.UpdateAsync(profile, cancellationToken);
        if (!updated)
        {
            logger.LogError("Fail updating profile; UserId: {UserId}", userId);
            throw new ServiceUnavailableException("Update Profile");
        }
        
        logger.LogInformation("Profile updated successfully. UserId: {UserId}", userId);
        return updated;
    }
    
    /// <inheritdoc />
    public async Task<bool> RequestAccountDeletionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await userRepository.DeleteAsync(userId, userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MonthlyQuotaStatusDto> GetMonthlyQuotaStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userUsageWithPlan = await usageRepository.GetUserUsageWithPlanIdAsync(userId, cancellationToken);
    
        if (userUsageWithPlan == null)
            throw new NotFoundException($"Usage statistics for user '{userId}' not found.");
    
        // Get plan limits from static configuration (no database hit!)
        if (!PlanConfiguration.Plans.TryGetValue(userUsageWithPlan.SubscriptionPlanId, out var planConfig))
        {
            throw new InvalidOperationException($"Invalid subscription plan: {userUsageWithPlan.SubscriptionPlanId}");
        }

        var monthlyQuotaStatusDto = CalculateQuotaStatus(userUsageWithPlan, planConfig);
        return monthlyQuotaStatusDto;
    }

    #region Helper Methods

    /// <summary>
    /// Calculates the user's monthly quota usage statistics and status indicators 
    /// based on their current usage and the limits defined by their subscription plan.
    /// </summary>
    /// <param name="usage">The user's usage data including links, QR codes, and reset date.</param>
    /// <param name="planConfig">The subscription plan configuration containing quota limits.</param>
    /// <returns>
    /// A <see cref="MonthlyQuotaStatusDto"/> containing detailed usage statistics,
    /// remaining quota values, usage percentages, and quota exhaustion flags.
    /// </returns>
    /// <remarks>
    /// This method calculates:
    /// <list type="bullet">
    ///   <item><description>Remaining link and QR code quota for the month</description></item>
    ///   <item><description>Percentage of usage for both links and QR codes</description></item>
    ///   <item><description>Days left until quota reset</description></item>
    ///   <item><description>Boolean flags indicating if usage is near or exceeds the allowed limit</description></item>
    /// </list>
    /// The quota warning threshold is fixed at 80%.
    /// </remarks>
    private static MonthlyQuotaStatusDto CalculateQuotaStatus(UserUsageWithPlan usage, PlanConfiguration planConfig)
    {
        const double quotaWarningThreshold = 80.0; // 80% usage threshold

        var maxLinks = planConfig.Limits[PlanConfiguration.enPlanLimits.UrlsPerMonth];
        var maxQrCodes = planConfig.Limits[PlanConfiguration.enPlanLimits.QrCodesPerMonth];
        
        // Calculate remaining quotas
        var remainingLinks = Math.Max(maxLinks - usage.MonthlyLinksCreated, 0);
        var remainingQrCodes = Math.Max(maxQrCodes - usage.MonthlyQrCodesCreated, 0);
        
        // Calculate usage percentages (avoid division by zero)
        var linksUsagePercentage = maxLinks > 0 ? (double)usage.MonthlyLinksCreated / maxLinks * 100.0 : 0.0;
        var qrCodesUsagePercentage = maxQrCodes > 0 ? (double)usage.MonthlyQrCodesCreated / maxQrCodes * 100.0 : 0.0;
        
        // Calculate days until reset
        var daysUntilReset = Math.Max((usage.MonthlyResetDate.Date - DateTime.UtcNow.Date).Days, 0);
        
        // Boolean flags
        var isLinksQuotaExhausted = remainingLinks <= 0;
        var isQrCodesQuotaExhausted = remainingQrCodes <= 0;
        var isNearLinksQuotaLimit = linksUsagePercentage >= quotaWarningThreshold;
        var isNearQrCodesQuotaLimit = qrCodesUsagePercentage >= quotaWarningThreshold;

        return new MonthlyQuotaStatusDto(
            remainingLinks,
            remainingQrCodes,
            daysUntilReset,
            maxLinks,
            maxQrCodes,
            usage.MonthlyLinksCreated,
            usage.MonthlyQrCodesCreated,
            usage.SubscriptionPlanId,
            usage.MonthlyResetDate,
            Math.Round(linksUsagePercentage, 2), // Round to 2 decimal places
            Math.Round(qrCodesUsagePercentage, 2),
            isLinksQuotaExhausted,
            isQrCodesQuotaExhausted,
            isNearLinksQuotaLimit,
            isNearQrCodesQuotaLimit
        );
    }


    #endregion
}
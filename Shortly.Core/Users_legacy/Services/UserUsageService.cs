using Microsoft.Extensions.Logging;
using Shortly.Core.Users_legacy.Contracts;
using Shortly.Domain.Users;

namespace Shortly.Core.Users_legacy.Services;

/// <summary>
///     Provides services for managing user usage statistics, including tracking link and QR code creation,
///     monitoring usage limits, and handling monthly usage resets.
/// </summary>
public class UserUsageService(IUserUsageRepository usageRepository, ILogger<UserUsageService> logger)
    : IUserUsageService
{
   
    /// <inheritdoc/>
    public async Task<int> ResetMonthlyUsageForAllAsync(CancellationToken cancellationToken = default)
    {
        var resetDate = DateTime.UtcNow;

        var affectedUsersCount = await usageRepository.ResetAllMonthlyAsync(cancellationToken);

        if (affectedUsersCount > 0)
        {
            logger.LogInformation("{Count} user usage records were reset successfully on {ResetDate}.",
                affectedUsersCount, resetDate.Date);
        }

        return affectedUsersCount;
    }

}
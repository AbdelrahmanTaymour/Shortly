
namespace Shortly.Core.Users_legacy.Contracts;

/// <summary>
///     Defines services for managing user usage statistics, including tracking link and QR code creation,
///     monitoring usage limits, and handling monthly usage resets.
/// </summary>
public interface IUserUsageService
{
    /// <summary>
    ///     Resets monthly usage statistics (links and QR codes created) for all non-deleted users whose reset date has passed.
    ///     This method is typically used in scheduled maintenance or batch operations to handle monthly billing cycles.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The count of user usage has been rested</returns>
    /// <remarks>
    ///     This operation logs information about the number of users affected and the reset date.
    ///     A warning is logged if no users matched the reset criteria.
    /// </remarks>
    Task<int> ResetMonthlyUsageForAllAsync(CancellationToken cancellationToken = default);

}
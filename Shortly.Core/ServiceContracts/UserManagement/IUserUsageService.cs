using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.Exceptions.ClientErrors;

namespace Shortly.Core.ServiceContracts.UserManagement;

/// <summary>
///     Defines services for managing user usage statistics, including tracking link and QR code creation,
///     monitoring usage limits, and handling monthly usage resets.
/// </summary>
public interface IUserUsageService
{
    /// <summary>
    ///     Retrieves comprehensive usage statistics for a specific user.
    ///     This method is functionally equivalent to <see cref="GetByUserIdAsync" /> but provides semantic clarity for usage
    ///     reporting scenarios.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="UserUsageDto" /> containing the user's comprehensive usage statistics.</returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified user ID.</exception>
    Task<UserUsageDto> GetUsageStatsAsync(Guid userId, CancellationToken cancellationToken = default);

    
    /// <summary>
    ///     Tracks the creation of a new link by incrementing the user's monthly link creation count.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who created the link.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the link creation was successfully tracked; otherwise, <c>false</c>.</returns>
    Task<bool> TrackLinkCreationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Tracks the creation of a new QR code by incrementing the user's monthly QR code creation count.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who created the QR code.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the QR code creation was successfully tracked; otherwise, <c>false</c>.</returns>
    Task<bool> TrackQrCodeCreationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Determines whether a user can create more links based on their current usage and subscription plan limits.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the user can create more links; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified user ID.</exception>
    Task<bool> CanCreateMoreLinksAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Determines whether a user can create more QR codes based on their current usage and subscription plan limits.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the user can create more QR codes; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified user ID.</exception>
    Task<bool> CanCreateMoreQrCodesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates the number of remaining links a user can create in the current monthly period.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of remaining links the user can create. Returns 0 if the limit has been reached or exceeded.</returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified user ID.</exception>
    Task<int> GetRemainingLinksAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates the number of remaining QR codes a user can create in the current monthly period.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of remaining QR codes the user can create. Returns 0 if the limit has been reached or exceeded.</returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified user ID.</exception>
    Task<int> GetRemainingQrCodesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether a user has exceeded their monthly limits for either links or QR codes based on their subscription
    ///     plan.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the user has exceeded any of their monthly limits; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown when no usage record is found for the specified user ID.</exception>
    Task<bool> HasExceededLimitsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets the monthly usage statistics (links and QR codes created) for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose usage should be reset.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the user's monthly usage was successfully reset; otherwise, <c>false</c>.</returns>
    Task<bool> ResetMonthlyUsageAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets monthly usage statistics (links and QR codes created) for all non-deleted users whose reset date has passed.
    ///     This method is typically used in scheduled maintenance or batch operations to handle monthly billing cycles.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if at least one user usage record was reset; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     This operation logs information about the number of users affected and the reset date.
    ///     A warning is logged if no users matched the reset criteria.
    /// </remarks>
    Task<bool> ResetMonthlyUsageForAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates a usage report for users whose reset dates fall within the specified date range.
    ///     This method is useful for analytics, billing, and administrative reporting purposes.
    /// </summary>
    /// <param name="from">The start date of the reporting period (inclusive).</param>
    /// <param name="to">The end date of the reporting period (inclusive).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    ///     An enumerable collection of <see cref="UserUsageDto" /> objects representing users' usage statistics within
    ///     the specified date range.
    /// </returns>
    /// <remarks>
    ///     The report includes users whose monthly reset date falls within the specified range,
    ///     regardless of when they actually used the service.
    /// </remarks>
    Task<IEnumerable<UserUsageDto>> GetUsageReportAsync(DateTime from, DateTime to,
        CancellationToken cancellationToken = default);
}
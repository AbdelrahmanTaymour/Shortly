using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.DTOs.UsersDTOs.Profile;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts.UserManagement;

/// <summary>
/// Defines operations related to user profile management and its usage statistics.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Retrieves the user profile associated with the specified user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="UserProfileDto"/> containing profile data.</returns>
    /// <exception cref="NotFoundException">Thrown if the profile is not found.</exception>
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the profile information for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="request">The updated profile data.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the profile was updated successfully; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown if the profile is not found.</exception>
    /// <exception cref="ServiceUnavailableException">Thrown if the update operation fails unexpectedly.</exception>
    Task<bool> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates an account deletion request for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the deletion request was successful; otherwise, <c>false</c>.</returns>
    Task<bool> RequestAccountDeletionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current monthly quota usage and limits for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="MonthlyQuotaStatusDto"/> containing quota usage and status details.</returns>
    /// <exception cref="NotFoundException">Thrown if the usage statistics are not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the subscription plan is invalid.</exception>
    Task<MonthlyQuotaStatusDto> GetMonthlyQuotaStatusAsync(Guid userId, CancellationToken cancellationToken = default);
}
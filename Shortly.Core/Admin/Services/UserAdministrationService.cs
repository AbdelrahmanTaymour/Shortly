using Shortly.Core.Admin.Contracts;
using Shortly.Core.Common;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Users.Contracts;
using Shortly.Core.Users.DTOs.User;
using Shortly.Domain.RepositoryContract.Organizations;

namespace Shortly.Core.Admin.Services;

/// <inheritdoc />
/// <param name="adminQueries">Repository for administrative user operations</param>
/// <param name="orgRepository">Repository for organization-related operations</param>
public class UserAdministrationService(
    IUserQueries adminQueries,
    IOrganizationRepository orgRepository) : IUserAdministrationService
{
    /// <inheritdoc />
    public async Task<ForceUpdateUserResponse> ForceUpdateUserAsync(Guid userId, ForceUpdateUserRequest request)
    {
        return await adminQueries.ForceUpdateUserAsync(userId, request);
    }

    /// <inheritdoc />
    public async Task<bool> HardDeleteUserAsync(Guid userId, bool deleteOwnedShortUrls,
        CancellationToken cancellationToken = default)
    {
        if (await orgRepository.IsUserOwnerOfAnyOrganization(userId))
            throw new BusinessRuleException("Cannot delete user with active organization membership.");

        return await adminQueries.HardDeleteUserAsync(userId, deleteOwnedShortUrls, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkActivateUsersAsync(ICollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        if (userIds.Count == 0)
            throw new ArgumentException("User IDs collection cannot be empty.", nameof(userIds));

        return await adminQueries.BulkActivateUsersAsync(userIds, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkDeactivateUsersAsync(ICollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        if (userIds.Count == 0)
            throw new ArgumentException("User IDs collection cannot be empty.", nameof(userIds));

        return await adminQueries.BulkDeactivateUsersAsync(userIds, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkDeleteUsersAsync(ICollection<Guid> userIds, Guid deletedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        if (deletedBy == Guid.Empty)
            throw new ArgumentException("DeletedBy user ID cannot be empty.", nameof(deletedBy));

        if (userIds.Count == 0)
            throw new ArgumentException("User IDs collection cannot be empty.", nameof(userIds));

        return await adminQueries.BulkDeleteUsersAsync(userIds, deletedBy, cancellationToken);
    }
}
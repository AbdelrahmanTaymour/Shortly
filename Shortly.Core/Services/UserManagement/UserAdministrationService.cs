using Shortly.Core.DTOs;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.UserManagement;

namespace Shortly.Core.Services.UserManagement;

/// <inheritdoc />
/// <param name="adminRepository">Repository for administrative user operations</param>
/// <param name="orgRepository">Repository for organization-related operations</param>
public class UserAdministrationService(
    IUserAdministrationRepository adminRepository,
    IOrganizationRepository orgRepository) : IUserAdministrationService
{
    /// <inheritdoc />
    public async Task<ForceUpdateUserResponse> ForceUpdateUserAsync(Guid userId, ForceUpdateUserRequest request)
    {
        return await adminRepository.ForceUpdateUserAsync(userId, request);
    }

    /// <inheritdoc />
    public async Task<bool> HardDeleteUserAsync(Guid userId, bool deleteOwnedShortUrls,
        CancellationToken cancellationToken = default)
    {
        if (await orgRepository.IsUserOwnerOfAnyOrganization(userId))
            throw new BusinessRuleException("Cannot delete user with active organization membership.");

        return await adminRepository.HardDeleteUserAsync(userId, deleteOwnedShortUrls, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkActivateUsersAsync(ICollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        if (userIds.Count == 0)
            throw new ArgumentException("User IDs collection cannot be empty.", nameof(userIds));

        return await adminRepository.BulkActivateUsersAsync(userIds, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkDeactivateUsersAsync(ICollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        if (userIds.Count == 0)
            throw new ArgumentException("User IDs collection cannot be empty.", nameof(userIds));

        return await adminRepository.BulkDeactivateUsersAsync(userIds, cancellationToken);
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

        return await adminRepository.BulkDeleteUsersAsync(userIds, deletedBy, cancellationToken);
    }
}
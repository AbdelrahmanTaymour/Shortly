using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.UserManagement;

public class UserAdministrationService: IUserAdministrationService
{
    public Task<UserDto> ForceUpdateUserAsync(Guid userId, UpdateUserDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HardDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<BulkOperationResult> BulkActivateUsersAsync(IEnumerable<Guid> userIds)
    {
        throw new NotImplementedException();
    }

    public Task<BulkOperationResult> BulkDeactivateUsersAsync(IEnumerable<Guid> userIds)
    {
        throw new NotImplementedException();
    }

    public Task<BulkOperationResult> BulkUpdateRoleAsync(IEnumerable<Guid> userIds, enUserRole role)
    {
        throw new NotImplementedException();
    }

    public Task<BulkOperationResult> BulkDeleteUsersAsync(IEnumerable<Guid> userIds, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
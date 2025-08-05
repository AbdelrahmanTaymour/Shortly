using Shortly.Core.DTOs.UsersDTOs.Search;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts.UserManagement;

public interface IUserAdministrationService
{
    // Admin-specific user management
    Task<UserDto> ForceUpdateUserAsync(Guid userId, UpdateUserDto dto);
    Task<bool> HardDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    // Bulk operations
    Task<BulkOperationResult> BulkActivateUsersAsync(IEnumerable<Guid> userIds);
    Task<BulkOperationResult> BulkDeactivateUsersAsync(IEnumerable<Guid> userIds);
    Task<BulkOperationResult> BulkUpdateRoleAsync(IEnumerable<Guid> userIds, enUserRole role);
    Task<BulkOperationResult> BulkDeleteUsersAsync(IEnumerable<Guid> userIds, Guid deletedBy, CancellationToken cancellationToken = default);
}

public record BulkOperationResult(
    int TotalProcessed,
    int SuccessCount,
    int FailureCount,
    IEnumerable<string> Errors
);

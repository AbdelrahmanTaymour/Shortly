using Shortly.Domain.Enums;

namespace Shortly.API.Authorization;

public interface IAuthorizationService
{
    enPermissions GetUserPermissions(Guid userId);
    bool HasPermission(Guid userId, enPermissions permission);
    bool HasAnyPermission(Guid userId, params enPermissions[] permissions);
    bool HasAllPermissions(Guid userId, params enPermissions[] permissions);
    bool CheckLimit(Guid userId, string limitType, int currentUsage);
    void GrantPermission(Guid userId, enPermissions permission);
    void RevokePermission(Guid userId, enPermissions permission);
    void ChangeUserRole(Guid userId, enPermissions newRole);
}
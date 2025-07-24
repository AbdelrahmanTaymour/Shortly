using Shortly.Domain.Enums;

namespace Shortly.Core.Extensions;

public static class PermissionExtensions
{
    public static bool HasPermission(this enPermissions userPermissions, enPermissions requiredPermission)
    {
        return (userPermissions & requiredPermission) == requiredPermission;
    }

    public static bool HasAnyPermission(this enPermissions userPermissions, params enPermissions[] requiredPermissions)
    {
        return requiredPermissions.Any(p => userPermissions.HasPermission(p));
    }

    public static bool HasAllPermissions(this enPermissions userPermissions, params enPermissions[] requiredPermissions)
    {
        return requiredPermissions.All(p => userPermissions.HasPermission(p));
    }

    public static enPermissions AddPermission(this enPermissions userPermissions, enPermissions permissionToAdd)
    {
        return userPermissions | permissionToAdd;
    }

    public static enPermissions RemovePermission(this enPermissions userPermissions, enPermissions permissionToRemove)
    {
        return userPermissions & ~permissionToRemove;
    }
}
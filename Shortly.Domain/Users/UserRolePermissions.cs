using Shortly.Domain.Enums;

namespace Shortly.Domain.Users;

/// <summary>
/// Pure, static, in-memory lookup of an account-level role → permission bitmask.
/// The only place <see cref="UserRole"/> is ever translated into <see cref="enPermissions"/>.
/// </summary>
public static class UserRolePermissions
{
    private static readonly Dictionary<UserRole, enPermissions> Map = new()
    {
        [UserRole.RegisteredUser] = enPermissions.BasicUser,
        [UserRole.SuperAdmin] = enPermissions.SuperAdmin,
        [UserRole.SystemAdmin] = enPermissions.SystemAdmin
    };

    public static enPermissions For(UserRole role) =>
        Map.TryGetValue(role, out var permissions) ? permissions : enPermissions.None;
}
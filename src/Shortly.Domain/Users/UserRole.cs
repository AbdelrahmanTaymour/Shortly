namespace Shortly.Domain.Users;

/// <summary>
/// Account-level role. Stored directly on <c>User.Role</c>. Drives permissions that apply
/// regardless of organization context (system/account administration).
/// </summary>
public enum UserRole : byte
{
    User = 1,
    SuperAdmin = 2,
    SystemAdmin = 3
}
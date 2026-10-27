using Microsoft.AspNetCore.Authorization;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.Authorization;

/// <summary>
/// Apply to a controller/action to require a specific permission, e.g.
/// [RequiredPermission(enPermissions.CreateLink)]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiredPermissionAttribute : AuthorizeAttribute
{
    public RequiredPermissionAttribute(enPermissions permission)
    {
        Permission = permission;
        Policy = permission.ToString();
    }

    public enPermissions Permission { get; }
}
using Microsoft.AspNetCore.Authorization;
using Shortly.Domain.Enums;

namespace Shortly.API.Authorization;

[AttributeUsage(AttributeTargets.Method)]
public class RequirePermissionAttribute: AuthorizeAttribute
{
    public enPermissions Permission { get; }

    public RequirePermissionAttribute(enPermissions permission): base(permission.ToString())
    {
        Permission = permission;
    }
}
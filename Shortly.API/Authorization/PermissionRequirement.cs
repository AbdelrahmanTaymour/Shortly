using Microsoft.AspNetCore.Authorization;
using Shortly.Domain.Enums;

namespace Shortly.API.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public enPermissions Permission { get; }

    public PermissionRequirement(enPermissions permission)
    {
        Permission = permission;
    }
}
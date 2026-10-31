using Microsoft.AspNetCore.Authorization;
using Shortly.Domain.Enums;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Authorization;

internal sealed class PermissionRequirement(enPermissions permission) : IAuthorizationRequirement
{
    public enPermissions Permission { get; } = permission;
}
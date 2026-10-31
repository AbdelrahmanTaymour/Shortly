using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Shortly.Domain.Enums;
using Shortly.Domain.Members;
using Shortly.Domain.Users;
using Shortly.Infrastructure.Authentication;
using Shortly.Infrastructure.Authorization;

namespace Shortly.Infrastructure.Authorization;

/// <summary>
/// Pure, in-memory permission check — no database or cache round-trip. Reads the role claim(s)
/// already present on the token and looks up the permission bitmask via the static maps.
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity is not { IsAuthenticated: true })
            return Task.CompletedTask;

        var permissions = enPermissions.None;

        if (context.User.GetUserRole() is { } userRole)
            permissions |= UserRolePermissions.For(userRole);

        if (context.User.GetMemberRole() is { } memberRole)
            permissions |= MemberRolePermissions.For(memberRole);

        if ((permissions & requirement.Permission) == requirement.Permission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
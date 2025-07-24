using Microsoft.AspNetCore.Authorization;

namespace Shortly.API.Authorization;

public class PermissionAuthorizationHandler: AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userPermissions = int.Parse(context.User.FindFirst("Permissions")?.Value ?? "0");
       
        if ((userPermissions & (int)requirement.Permission) == (int)requirement.Permission)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
using Microsoft.AspNetCore.Authorization;

namespace Shortly.API.Authorization;

public class PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger) 
    : AuthorizationHandler<PermissionRequirement>
{
    
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissionsClaim = context.User.FindFirst("Permissions")?.Value;
        
        if (string.IsNullOrEmpty(permissionsClaim))
        {
            logger.LogWarning("No permissions claim found for user");
            return Task.CompletedTask;
        }

        try
        {
            // Parse as long since your enum is long-based
            var userPermissions = long.Parse(permissionsClaim);
            var requiredPermission = (long)requirement.Permission;

            // Use bitwise AND to check if user has the required permission
            if ((userPermissions & requiredPermission) == requiredPermission)
            {
                context.Succeed(requirement);
                logger.LogDebug("Permission check succeeded for {Permission}", requirement.Permission);
            }
            else
            {
                logger.LogWarning("Permission check failed. User has {UserPermissions}, required {RequiredPermission}", 
                    userPermissions, requiredPermission);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing permissions claim: {PermissionsClaim}", permissionsClaim);
        }

        return Task.CompletedTask;
    }
}
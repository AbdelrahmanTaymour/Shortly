using Microsoft.AspNetCore.Authorization;
using Shortly.Domain.Enums;

namespace Shortly.API.Authorization;

public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddUrlShortenerAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Register permission policies for individual permissions
            foreach (var permission in Enum.GetValues<enPermissions>())
            {
                // Skip None and AllPermissions, and skip combination permissions to avoid policy conflicts
                if (permission != enPermissions.None && 
                    permission != enPermissions.SystemAdmin &&
                    !IsCombinationPermission(permission))
                {
                    options.AddPolicy(permission.ToString(),
                        policy => policy.Requirements.Add(new PermissionRequirement(permission)));
                }
            }
        });

        // Register the authorization handler
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }

    private static bool IsCombinationPermission(enPermissions permission)
    {
        // List of combination permissions that shouldn't have individual policies
        var combinations = new[]
        {
            enPermissions.BasicUser,
            enPermissions.TeamMember,
            enPermissions.TeamManager,
            enPermissions.OrgAdmin,
            enPermissions.SuperAdmin,
            enPermissions.SystemAdmin
        };

        return combinations.Contains(permission);
    }
}
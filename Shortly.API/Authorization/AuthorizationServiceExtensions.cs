using Shortly.Domain.Enums;

namespace Shortly.API.Authorization;

public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddUrlShortenerAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Register permission policies
            foreach (enPermissions permission in Enum.GetValues<enPermissions>())
            {
                if (permission != enPermissions.None && permission != enPermissions.AllPermissions)
                {
                    options.AddPolicy(permission.ToString(),
                        policy => policy.Requirements.Add(new PermissionRequirement(permission)));
                }
            }
        });

        return services;
    }
}
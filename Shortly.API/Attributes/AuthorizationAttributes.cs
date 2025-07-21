using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shortly.Core.Constants;
using Shortly.Core.ServiceContracts;
using System.Security.Claims;

namespace Shortly.API.Attributes;

/// <summary>
/// Authorization attribute for permission-based access control
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _permissions;
    private readonly bool _requireAll;

    public RequirePermissionAttribute(string permission)
    {
        _permissions = new[] { permission };
        _requireAll = true;
    }

    public RequirePermissionAttribute(params string[] permissions)
    {
        _permissions = permissions;
        _requireAll = false; // Default to requiring any permission
    }

    public RequirePermissionAttribute(bool requireAll, params string[] permissions)
    {
        _permissions = permissions;
        _requireAll = requireAll;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         context.HttpContext.User.FindFirst("sub")?.Value ??
                         context.HttpContext.User.FindFirst("id")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get authorization service
        var authorizationService = context.HttpContext.RequestServices.GetService<IAuthorizationService>();
        if (authorizationService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Check permissions
        bool hasAccess;
        if (_requireAll)
        {
            hasAccess = await authorizationService.HasAllPermissionsAsync(userId, _permissions);
        }
        else
        {
            hasAccess = await authorizationService.HasAnyPermissionAsync(userId, _permissions);
        }

        if (!hasAccess)
        {
            context.Result = new ForbidResult();
        }
    }
}

/// <summary>
/// Authorization attribute for role-based access control
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _roles;
    private readonly bool _requireAll;

    public RequireRoleAttribute(string role)
    {
        _roles = new[] { role };
        _requireAll = true;
    }

    public RequireRoleAttribute(params string[] roles)
    {
        _roles = roles;
        _requireAll = false; // Default to requiring any role
    }

    public RequireRoleAttribute(bool requireAll, params string[] roles)
    {
        _roles = roles;
        _requireAll = requireAll;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         context.HttpContext.User.FindFirst("sub")?.Value ??
                         context.HttpContext.User.FindFirst("id")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get authorization service
        var authorizationService = context.HttpContext.RequestServices.GetService<IAuthorizationService>();
        if (authorizationService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Check roles
        bool hasAccess = false;
        if (_requireAll)
        {
            hasAccess = true;
            foreach (var role in _roles)
            {
                if (!await authorizationService.HasRoleAsync(userId, role))
                {
                    hasAccess = false;
                    break;
                }
            }
        }
        else
        {
            foreach (var role in _roles)
            {
                if (await authorizationService.HasRoleAsync(userId, role))
                {
                    hasAccess = true;
                    break;
                }
            }
        }

        if (!hasAccess)
        {
            context.Result = new ForbidResult();
        }
    }
}

/// <summary>
/// Authorization attribute for resource ownership validation
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequireResourceOwnershipAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _resourceIdParameter;
    private readonly string _resourceType;

    public RequireResourceOwnershipAttribute(string resourceType, string resourceIdParameter = "id")
    {
        _resourceType = resourceType;
        _resourceIdParameter = resourceIdParameter;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         context.HttpContext.User.FindFirst("sub")?.Value ??
                         context.HttpContext.User.FindFirst("id")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get resource ID from route/query parameters
        var resourceId = context.RouteData.Values[_resourceIdParameter]?.ToString() ??
                        context.HttpContext.Request.Query[_resourceIdParameter].ToString();

        if (string.IsNullOrEmpty(resourceId))
        {
            context.Result = new BadRequestObjectResult($"Resource ID parameter '{_resourceIdParameter}' is required");
            return;
        }

        // Get authorization service
        var authorizationService = context.HttpContext.RequestServices.GetService<IAuthorizationService>();
        if (authorizationService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Check resource access
        var hasAccess = await authorizationService.CanAccessResourceAsync(userId, _resourceType, resourceId, "read");
        if (!hasAccess)
        {
            context.Result = new ForbidResult();
        }
    }
}

/// <summary>
/// Authorization attribute for subscription tier requirements
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSubscriptionTierAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly SubscriptionTier _minimumTier;

    public RequireSubscriptionTierAttribute(SubscriptionTier minimumTier)
    {
        _minimumTier = minimumTier;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         context.HttpContext.User.FindFirst("sub")?.Value ??
                         context.HttpContext.User.FindFirst("id")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get user management service
        var userService = context.HttpContext.RequestServices.GetService<IUserManagementService>();
        if (userService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Get user and check subscription tier
        var user = await userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (user.CurrentTier < _minimumTier)
        {
            context.Result = new ObjectResult(new
            {
                Error = "Insufficient subscription tier",
                Required = _minimumTier.ToString(),
                Current = user.CurrentTier.ToString(),
                Message = $"This feature requires {_minimumTier} subscription or higher"
            })
            {
                StatusCode = 402 // Payment Required
            };
        }
    }
}

/// <summary>
/// Authorization attribute for admin-only access
/// </summary>
public class RequireAdminAttribute : RequireRoleAttribute
{
    public RequireAdminAttribute() : base(DefaultRoles.SuperAdmin, DefaultRoles.Admin) { }
}

/// <summary>
/// Authorization attribute for super admin-only access
/// </summary>
public class RequireSuperAdminAttribute : RequireRoleAttribute
{
    public RequireSuperAdminAttribute() : base(DefaultRoles.SuperAdmin) { }
}

/// <summary>
/// Authorization attribute for moderator-level access
/// </summary>
public class RequireModeratorAttribute : RequireRoleAttribute
{
    public RequireModeratorAttribute() : base(DefaultRoles.SuperAdmin, DefaultRoles.Admin, DefaultRoles.Moderator) { }
}

/// <summary>
/// Authorization attribute for API access
/// </summary>
public class RequireApiAccessAttribute : RequirePermissionAttribute
{
    public RequireApiAccessAttribute() : base(DefaultPermissions.UseApi) { }
}

/// <summary>
/// Authorization attribute for advanced API access
/// </summary>
public class RequireAdvancedApiAccessAttribute : RequirePermissionAttribute
{
    public RequireAdvancedApiAccessAttribute() : base(DefaultPermissions.UseAdvancedApi) { }
}

/// <summary>
/// Conditional authorization based on feature flags
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireFeatureAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _featureName;

    public RequireFeatureAttribute(string featureName)
    {
        _featureName = featureName;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get configuration to check feature flags
        var configuration = context.HttpContext.RequestServices.GetService<IConfiguration>();
        if (configuration == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        var isFeatureEnabled = configuration.GetValue<bool>($"Features:{_featureName}", false);
        if (!isFeatureEnabled)
        {
            context.Result = new ObjectResult(new
            {
                Error = "Feature not available",
                Feature = _featureName,
                Message = "This feature is currently disabled"
            })
            {
                StatusCode = 501 // Not Implemented
            };
        }
    }
}
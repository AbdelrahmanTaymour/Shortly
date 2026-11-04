using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Shortly.Api.Extensions;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.Abstractions;
using Shortly.Infrastructure.Authentication;

namespace Shortly.Api.Routing;

/// <summary>
/// Marks a controller to automatically expose its actions under an organization-scoped route path.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class OrganizationScopedRoutesAttribute(string segment) : Attribute
{
    public string Segment { get; } = segment;
}

/// <summary>
/// Scans controllers for <see cref="OrganizationScopedRoutesAttribute"/> and duplicates their routes with an organization prefix.
/// </summary>
public sealed class OrganizationScopedRoutesConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var attribute = controller.Attributes.OfType<OrganizationScopedRoutesAttribute>().SingleOrDefault();
        if (attribute is null) return;

        foreach (var action in controller.Actions)
        {
            foreach (var selector in action.Selectors.Where(s => s.AttributeRouteModel is not null).ToList())
            {
                var actionTemplate = selector.AttributeRouteModel!.Template;
                
                // Skip absolute templates that override the root path
                if (actionTemplate is not null && actionTemplate.StartsWith("~/", StringComparison.Ordinal)) 
                    continue;

                // Clone the route selector and prepend the organization segment
                var scopedSelector = new SelectorModel(selector)
                {
                    AttributeRouteModel = new AttributeRouteModel
                    {
                        Template = $"~/api/organizations/{{organizationId:guid}}/{attribute.Segment}" +
                                   (string.IsNullOrEmpty(actionTemplate) ? "" : "/" + actionTemplate)
                    }
                };

                action.Selectors.Add(scopedSelector);
            }
        }
    }
}

/// <summary>
/// Intercepts requests on organization-scoped routes to validate user membership and set the request context.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class OrganizationRouteScopeAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // If the URL does not contain an organization ID, skip scoping checks
        if (context.RouteData.Values["organizationId"] is not string raw || !Guid.TryParse(raw, out var organizationId))
        {
            await next();
            return;
        }

        var httpContext = context.HttpContext;

        // Ensure the caller is logged in
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Verify that the user is an active member of the target organization
        var authService = httpContext.RequestServices.GetRequiredService<IOrganizationAuthorizationService>();
        var membership = await authService.RequireMembershipAsync(organizationId, httpContext.RequestAborted);
        
        if (membership.IsFailure)
        {
            context.Result = Result.Failure(membership.Error).ToProblemDetails();
            return;
        }

        // Bind the organization ID to the current request context scope
        RequestOrganizationScope.Set(httpContext, organizationId);
        await next();
    }
}
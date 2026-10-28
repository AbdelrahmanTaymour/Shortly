using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is not null) return policy;

        if (!Enum.TryParse<enPermissions>(policyName, out var permission))
            return null;

        return new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();
    }
}
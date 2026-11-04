using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Shortly.Api.Security;
using Shortly.Infrastructure.EdgeProtection.Models;
using Shortly.Infrastructure.EdgeProtection.Services;

namespace Shortly.Api.Extensions;

public static class EdgeProtectionExtensions
{
    public static void AddEdgeProtection(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureForwardedHeaders(services, configuration);
        ConfigureRateLimitingState(services, configuration);
        ConfigureRateLimitingPolicies(services, configuration);
    }

    private static void ConfigureForwardedHeaders(IServiceCollection services, IConfiguration configuration)
    {
        var forwardedSettings = configuration.GetSection(ForwardedHeadersSettings.SectionName)
            .Get<ForwardedHeadersSettings>() ?? new ForwardedHeadersSettings();
        
        forwardedSettings.Validate();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = forwardedSettings.ForwardLimit;

            if (forwardedSettings.TrustAllProxies)
            {
                options.KnownProxies.Clear();
                options.KnownNetworks.Clear();
            }

            foreach (var proxy in forwardedSettings.KnownProxies)
                options.KnownProxies.Add(IPAddress.Parse(proxy));

            foreach (var network in forwardedSettings.KnownNetworks)
                options.KnownNetworks.Add(ForwardedHeadersSettings.ParseNetwork(network));
        });
    }

    private static void ConfigureRateLimitingState(IServiceCollection services, IConfiguration configuration)
    {
        var perLinkOptions = configuration.GetSection(PasswordVerificationPerLinkRateLimitOptions.SectionName)
            .Get<PasswordVerificationPerLinkRateLimitOptions>() ?? new PasswordVerificationPerLinkRateLimitOptions();

        // Singleton stateful limiter for cross-client password brute-force tracking
        services.AddSingleton(new LinkPasswordAttemptLimiter(perLinkOptions));
    }

    private static void ConfigureRateLimitingPolicies(IServiceCollection services, IConfiguration configuration)
    {
        // Explicitly bind strongly-typed configuration options (eliminates magic strings/reflection)
        var anonymousCreation = BindOptions<AnonymousShortUrlCreationRateLimitOptions>(configuration);
        var passwordVerification = BindOptions<PasswordVerificationRateLimitOptions>(configuration);
        var login = BindOptions<LoginRateLimitOptions>(configuration);
        var registration = BindOptions<RegistrationRateLimitOptions>(configuration);
        var accountEmail = BindOptions<AccountEmailRateLimitOptions>(configuration);
        var availability = BindOptions<ShortCodeAvailabilityRateLimitOptions>(configuration);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Policy definitions
            options.AddPolicy(AnonymousShortUrlCreationRateLimitOptions.PolicyName, httpContext =>
            {
                if (httpContext.User.Identity?.IsAuthenticated == true)
                    return RateLimitPartition.GetNoLimiter("authenticated");

                return CreateFixedWindowPartition(httpContext, anonymousCreation.PermitLimit, anonymousCreation.WindowSeconds);
            });

            options.AddPolicy(PasswordVerificationRateLimitOptions.PolicyName, httpContext =>
                CreateFixedWindowPartition(httpContext, passwordVerification.PermitLimit, passwordVerification.WindowSeconds));

            options.AddPolicy(LoginRateLimitOptions.PolicyName, httpContext =>
                CreateFixedWindowPartition(httpContext, login.PermitLimit, login.WindowSeconds));

            options.AddPolicy(RegistrationRateLimitOptions.PolicyName, httpContext =>
                CreateFixedWindowPartition(httpContext, registration.PermitLimit, registration.WindowSeconds));

            options.AddPolicy(AccountEmailRateLimitOptions.PolicyName, httpContext =>
                CreateFixedWindowPartition(httpContext, accountEmail.PermitLimit, accountEmail.WindowSeconds));

            options.AddPolicy(ShortCodeAvailabilityRateLimitOptions.PolicyName, httpContext =>
                CreateFixedWindowPartition(httpContext, availability.PermitLimit, availability.WindowSeconds));

            // Delegate rejection handling to dedicated architecture component
            options.OnRejected = async (context, cancellationToken) =>
            {
                AttachRetryAfterHeader(context);
                await RateLimitRejectionHandler.HandleAsync(context.HttpContext, cancellationToken);
            };
        });
    }

    private static TOptions BindOptions<TOptions>(IConfiguration configuration) where TOptions : new()
    {
        // Clean mapping using the explicit constant section name convention per option class
        var sectionNameField = typeof(TOptions).GetField("SectionName")?.GetValue(null)?.ToString() 
                               ?? typeof(TOptions).Name;
        
        return configuration.GetSection(sectionNameField).Get<TOptions>() ?? new TOptions();
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(HttpContext context, int permitLimit, int windowSeconds) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientIpPartitionKey.From(context.Connection.RemoteIpAddress), 
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });

    private static void AttachRetryAfterHeader(OnRejectedContext context)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
            context.HttpContext.Response.Headers.RetryAfter = seconds.ToString();
        }
    }
}
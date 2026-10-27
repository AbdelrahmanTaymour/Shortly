using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Abstractions.Behaviors;
using Shortly.Core.Admin.Contracts;
using Shortly.Core.Admin.Services;
using Shortly.Core.Analytics.Contracts;
using Shortly.Core.Analytics.Services;
using Shortly.Core.ClickTracking.Contracts;
using Shortly.Core.ClickTracking.Services;
using Shortly.Core.Common;
using Shortly.Core.Invitations.Contracts;
using Shortly.Core.Invitations.Services;
using Shortly.Core.Members.Contracts;
using Shortly.Core.Members.Services;
using Shortly.Core.Organizations_legacy.Contracts;
using Shortly.Core.Organizations_legacy.Services;
using Shortly.Core.Profile.Contracts;
using Shortly.Core.Profile.Services;
using Shortly.Core.Security.Contracts;
using Shortly.Core.Security.Services;
using Shortly.Core.ShortUrls.Contracts;
using Shortly.Core.ShortUrls.Services;
using Shortly.Core.Teams.Contracts;
using Shortly.Core.Teams.Services;
using Shortly.Core.UserAuditLog.Contracts;
using Shortly.Core.UserAuditLog.Services;
using Shortly.Core.Users_legacy.Contracts;
using Shortly.Core.Users_legacy.Services;

namespace Shortly.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // URL Management
        services.AddScoped<IShortUrlsService, ShortUrlsService>();
        services.AddScoped<IShortUrlRedirectService, ShortUrlRedirectService>();
        services.AddScoped<IShortUrlQueryService, ShortUrlQueryService>();
        services.AddScoped<IShortUrlAnalyticsService, ShortUrlAnalyticsService>();
        services.AddScoped<IUrlBulkOperationsService, UrlBulkOperationsService>();
        services.AddScoped<IUrlStatisticsService, UrlStatisticsService>();

        // User Management (legacy, not yet migrated to CQRS)
        services.AddScoped<IUserSecurityService, UserSecurityService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IUserUsageService, UserUsageService>();
        services.AddScoped<IUserAuditLogService, UserAuditLogService>();

        // Click Event
        services.AddSingleton<ClickTrackingQueueService>();
        services.AddScoped<IClickTrackingService, ClickTrackingService>();
        services.AddScoped<ITrafficSourceAnalyzer, TrafficSourceAnalyzer>();
        services.AddScoped<IUserAgentParsingService, UserAgentParsingService>();

        // Organization Management (legacy, not yet migrated to CQRS)
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationUsageService, OrganizationUsageService>();
        services.AddScoped<IOrganizationMemberService, OrganizationMemberService>();
        services.AddScoped<IOrganizationTeamService, OrganizationTeamService>();
        services.AddScoped<IOrganizationInvitationService, OrganizationInvitationService>();

        return services;
    }
}
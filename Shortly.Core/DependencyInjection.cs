using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Admin.Contracts;
using Shortly.Core.Admin.Services;
using Shortly.Core.Analytics.Contracts;
using Shortly.Core.Analytics.Services;
using Shortly.Core.Auth.Contracts;
using Shortly.Core.Auth.Services;
using Shortly.Core.Auth.Validators;
using Shortly.Core.ClickTracking.Contracts;
using Shortly.Core.ClickTracking.Services;
using Shortly.Core.Common;
using Shortly.Core.Email.Contracts;
using Shortly.Core.Email.Services;
using Shortly.Core.Invitations.Contracts;
using Shortly.Core.Invitations.Services;
using Shortly.Core.Members.Contracts;
using Shortly.Core.Members.Services;
using Shortly.Core.Organizations.Contracts;
using Shortly.Core.Organizations.Services;
using Shortly.Core.Profile.Contracts;
using Shortly.Core.Profile.Services;
using Shortly.Core.Security.Contracts;
using Shortly.Core.Security.Services;
using Shortly.Core.ShortUrls.Contracts;
using Shortly.Core.ShortUrls.Services;
using Shortly.Core.Teams.Contracts;
using Shortly.Core.Teams.Services;
using Shortly.Core.Tokens.Contracts;
using Shortly.Core.Tokens.Services;
using Shortly.Core.UserAuditLog.Contracts;
using Shortly.Core.UserAuditLog.Services;
using Shortly.Core.Users.Contracts;
using Shortly.Core.Users.Services;

namespace Shortly.Core;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Core layer services and abstractions used by the Shortly application.
    /// </summary>
    /// <remarks>
    /// This method is responsible for registering domain-level services such as validators,
    /// business rules, policies, or any other cross-cutting concerns that belong to the Core domain.
    /// Typically, the Core layer contains only pure business logic and abstractions, but if there are
    /// any services to be registered, this is the place.
    /// </remarks>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to which Core services will be added.
    /// </param>
    /// <param name="configuration"></param>
    /// <returns>
    /// The modified <see cref="IServiceCollection"/> with registered Core services.
    /// </returns>
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Core-related services.
        
        // Email
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        services.AddSingleton<EmailQueueService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();

        // Auth
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserActionTokenService, UserActionTokenService>();
        services.AddScoped<IEmailChangeTokenService, EmailChangeTokenService>();
        services.AddScoped<IAccountService, AccountService>();
        
        // URL Management
        services.AddScoped<IShortUrlsService, ShortUrlsService>();
        services.AddScoped<IShortUrlRedirectService, ShortUrlRedirectService>();
        services.AddScoped<IShortUrlQueryService, ShortUrlQueryService>();
        services.AddScoped<IShortUrlAnalyticsService, ShortUrlAnalyticsService>();
        services.AddScoped<IUrlBulkOperationsService, UrlBulkOperationsService>();
        services.AddScoped<IUrlStatisticsService, UrlStatisticsService>();
        
        // User Management
        services.AddScoped<IUserSecurityService, UserSecurityService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IUserUsageService, UserUsageService>();
        services.AddScoped<IUserAuditLogService, UserAuditLogService>();
        services.AddScoped<IOAuthService, OAuthService>();
        
        // Click Event
        services.AddSingleton<ClickTrackingQueueService>();
        services.AddScoped<IClickTrackingService, ClickTrackingService>();
        services.AddScoped<ITrafficSourceAnalyzer, TrafficSourceAnalyzer>();
        services.AddScoped<IUserAgentParsingService, UserAgentParsingService>();
        services.AddScoped<IUrlStatisticsService, UrlStatisticsService>();
        
        // Organization Management
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationUsageService, OrganizationUsageService>();
        services.AddScoped<IOrganizationMemberService, OrganizationMemberService>();
        services.AddScoped<IOrganizationTeamService, OrganizationTeamService>();
        services.AddScoped<IOrganizationInvitationService, OrganizationInvitationService>();
        
        // Validations
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();


        return services;
    }
}
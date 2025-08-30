using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Models;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Core.ServiceContracts.ClickTracking;
using Shortly.Core.ServiceContracts.Email;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Core.ServiceContracts.Tokens;
using Shortly.Core.ServiceContracts.UrlManagement;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Core.Services.Authentication;
using Shortly.Core.Services.ClickTracking;
using Shortly.Core.Services.Email;
using Shortly.Core.Services.OrganizationManagement;
using Shortly.Core.Services.Tokens;
using Shortly.Core.Services.UrlManagement;
using Shortly.Core.Services.UserManagement;
using Shortly.Core.Validators.Auth;

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
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();

        // Auth
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserActionTokenService, UserActionTokenService>();
        
        // URL Management
        services.AddScoped<IShortUrlsService, ShortUrlsService>();
        services.AddScoped<IShortUrlRedirectService, ShortUrlRedirectService>();
        services.AddScoped<IShortUrlQueryService, ShortUrlQueryService>();
        services.AddScoped<IShortUrlAnalyticsService, ShortUrlAnalyticsService>();
        services.AddScoped<IUrlBulkOperationsService, UrlBulkOperationsService>();
        
        // User Management
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
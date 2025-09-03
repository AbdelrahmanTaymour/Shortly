using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shortly.Core.Models;
using Shortly.Core.RepositoryContract.Tokens;
using Shortly.Core.RepositoryContract.ClickTracking;
using Shortly.Core.RepositoryContract.EmailService;
using Shortly.Core.RepositoryContract.UrlManagement;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Infrastructure.BackgroundServices;
using Shortly.Infrastructure.DbContexts;
using Shortly.Infrastructure.Repositories.Tokens;
using Shortly.Infrastructure.Repositories.ClickTracking;
using Shortly.Infrastructure.Repositories.OrganizationManagement;
using Shortly.Infrastructure.Repositories.UrlManagement;
using Shortly.Infrastructure.Repositories.UserManagement;
using Shortly.Infrastructure.ScheduledJobs;
using Shortly.Infrastructure.Services;

namespace Shortly.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the dependencies defined in the Infrastructure layer of the Shortly application.
    /// </summary>
    /// <remarks>
    /// This method is intended to encapsulate all service registrations related to infrastructure concerns,
    /// such as database access, external services, file systems, and other low-level operations.
    /// It should be called from the application's composition root (e.g., Program.cs).
    /// </remarks>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> instance to which the infrastructure services will be added.
    /// </param>
    /// <param name="configuration"></param>
    /// <returns>
    /// The same <see cref="IConfiguration"/> instance, allowing for fluent chaining of service registrations.
    /// </returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register infrastructure-related services.
        services.AddDbContext<SQLServerDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ConnectionString")));
        
        
        // Register an email provider (can be configured to use different providers)
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        var emailProvider = configuration["EmailSettings:Provider"]?.ToLower() ?? "smtp";
        switch (emailProvider)
        {
            case "smtp":
                services.AddScoped<IEmailProvider, SmtpEmailProvider>();
                break;
            // Add other providers like SendGrid, AWS SES, etc.
            default:
                services.AddScoped<IEmailProvider, SmtpEmailProvider>();
                break;
        }
        
        // Register Quartz services
        services.AddQuartz(q =>
        {
            // Use Microsoft DI for Quartz
            q.UseMicrosoftDependencyInjectionJobFactory();

            // Register the MonthlyUsageResetJob with a Cron trigger
            q.ScheduleJob<MonthlyUsageResetJob>(trigger => trigger
                .WithIdentity("MonthlyUsageResetTrigger")
                .StartNow()
                .WithCronSchedule("0 0 1 * * ?")); // The first day of every month at 00:00
        });

        // Add Quartz.NET hosted service
        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true; // Wait for jobs to complete on shutdown
        });

        // Register the MonthlyUsageResetJob
        services.AddScoped<MonthlyUsageResetJob>();


        // User Management
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserSecurityRepository, UserSecurityRepository>();
        services.AddScoped<IUserUsageRepository, UserUsageRepository>();
        services.AddScoped<IUserAuditLogRepository, UserAuditLogRepository>();
        services.AddScoped<IUserAdministrationRepository, UserAdministrationRepository>();
        
        // Tokens
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserActionTokenRepository, UserActionTokenRepository>();
        services.AddScoped<IEmailChangeTokenRepository, EmailChangeTokenRepository>();
        
        // Url Management
        services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
        services.AddScoped<IUrlBulkOperationsRepository, UrlBulkOperationsRepository>();
        services.AddScoped<IShortUrlQueryRepository, ShortUrlQueryRepository>();
        services.AddScoped<IShortUrlRedirectRepository, ShortUrlRedirectRepository>();
        services.AddScoped<IShortUrlAnalyticsRepository, ShortUrlAnalyticsRepository>();
        
        // Click Event
        services.AddScoped<IClickEventRepository, ClickEventRepository>();
        services.AddHttpClient<IGeoLocationService, GeoLocationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "Short.ly/1.0");
        });

        // Organization Management
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationUsageRepository, OrganizationUsageRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IOrganizationTeamRepository, OrganizationTeamRepository>();
        services.AddScoped<IOrganizationTeamMemberRepository, OrganizationTeamMemberRepository>();
        services.AddScoped<IOrganizationInvitationRepository, OrganizationInvitationRepository>();

        
        // Register background services
        services.AddHostedService<EmailBackgroundWorker>();
        services.AddHostedService<ClickTrackingBackgroundWorker>();

        return services;
    }
}
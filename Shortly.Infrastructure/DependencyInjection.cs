using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shortly.Core.Analytics.CacheContracts;
using Shortly.Core.Analytics.Contracts;
using Shortly.Core.ClickTracking.Contracts;
using Shortly.Core.Email.Contracts;
using Shortly.Core.Email.DTOs;
using Shortly.Core.Email.Services;
using Shortly.Core.Organizations.Contracts;
using Shortly.Core.ShortUrls.Contracts;
using Shortly.Core.Users.Contracts;
using Shortly.Domain.RepositoryContract.Analytics;
using Shortly.Domain.RepositoryContract.ClickTracking;
using Shortly.Domain.RepositoryContract.Invitations;
using Shortly.Domain.RepositoryContract.Members;
using Shortly.Domain.RepositoryContract.Organizations;
using Shortly.Domain.RepositoryContract.Profile;
using Shortly.Domain.RepositoryContract.Security;
using Shortly.Domain.RepositoryContract.ShortUrls;
using Shortly.Domain.RepositoryContract.Teams;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.RepositoryContract.UserAuditLog;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Infrastructure.BackgroundServices;
using Shortly.Infrastructure.Caching.Analytics;
using Shortly.Infrastructure.DbContexts;
using Shortly.Infrastructure.Repositories.Analytics;
using Shortly.Infrastructure.Repositories.Tokens;
using Shortly.Infrastructure.Repositories.ClickTracking;
using Shortly.Infrastructure.Repositories.Invetations;
using Shortly.Infrastructure.Repositories.Memebrs;
using Shortly.Infrastructure.Repositories.Organizations;
using Shortly.Infrastructure.Repositories.Profile;
using Shortly.Infrastructure.Repositories.Security;
using Shortly.Infrastructure.Repositories.ShortUrls;
using Shortly.Infrastructure.Repositories.Teams;
using Shortly.Infrastructure.Repositories.UserAuditLog;
using Shortly.Infrastructure.Repositories.Users;
using Shortly.Infrastructure.ScheduledJobs;
using Shortly.Infrastructure.Services;
using Shortly.Infrastructure.Services.Email;
using Shortly.Infrastructure.Services.GeoLocation;

namespace Shortly.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContextFactory
        services.AddDbContextFactory<SqlServerDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions
                    .EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)
                    .CommandTimeout(30)
            ));
 
        services.AddScoped(sp =>
            sp.GetRequiredService<IDbContextFactory<SqlServerDbContext>>().CreateDbContext());
 
        // Email
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailProvider, SmtpEmailProvider>();
 
        // Quartz scheduled jobs
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            q.ScheduleJob<MonthlyUsageResetJob>(trigger => trigger
                .WithIdentity("MonthlyUsageResetTrigger")
                .StartNow()
                .WithCronSchedule("0 0 1 * * ?"));
        });
        services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
        services.AddScoped<MonthlyUsageResetJob>();
 
        // User Management
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserSecurityRepository, UserSecurityRepository>();
        services.AddScoped<IUserUsageRepository, UserUsageRepository>();
        services.AddScoped<IUserAuditLogRepository, UserAuditLogRepository>();
        services.AddScoped<IUserQueries, UserQueries>();
 
        // Tokens
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserActionTokenRepository, UserActionTokenRepository>();
        services.AddScoped<IEmailChangeTokenRepository, EmailChangeTokenRepository>();
 
        // URL Management
        services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
        services.AddScoped<IUrlBulkOperationsCommand, UrlBulkOperationsCommand>();
        services.AddScoped<IShortUrlQueryRepository, ShortUrlQueryRepository>();
        services.AddScoped<IShortUrlRedirectRepository, ShortUrlRedirectRepository>();
        services.AddScoped<IShortUrlRedirectQueries, ShortUrlRedirectRepository>();
        
        // ── Analytics ──
        services.AddAnalyticsServices();
 
        // Click Tracking
        services.AddScoped<IClickEventRepository, ClickEventRepository>();
        services.AddHttpClient<IGeoLocationService, GeoLocationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "Short.ly/1.0");
        });
 
        // Organization Management
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationUsageRepository, OrganizationUsageRepository>();
        services.AddScoped<IOrganizationUsageQeries, OrganizationUsageRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IOrganizationTeamRepository, OrganizationTeamRepository>();
        services.AddScoped<IOrganizationTeamMemberRepository, OrganizationTeamMemberRepository>();
        services.AddScoped<IOrganizationInvitationRepository, OrganizationInvitationRepository>();
 
        // Caching infrastructure
        services.AddMemoryCache(options =>
        {
            options.SizeLimit               = 10_000;
            options.CompactionPercentage    = 0.20;
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });
 
        var rawRedis = configuration.GetConnectionString("REDIS_CONNECTION_STRING") ?? "shortly-cache:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = rawRedis;
            options.InstanceName  = "Shortly:"; 
        });
 
        services.AddHostedService<EmailBackgroundWorker>();
        services.AddHostedService<ClickTrackingBackgroundWorker>();
    }
    
    private static void AddAnalyticsServices(this IServiceCollection services)
    {
        // 1. Hybrid L1/L2 Cache Service
        services.AddSingleton<ICachedStatisticsService, CachedStatisticsService>();
 
        // 2. High-Performance Dapper Query
        services.AddScoped<IClickStatisticsDapperQueries, ClickStatisticsDapperQueries>();

        // 3. Base Analytics Implementation
        // We register the concrete type first so both interfaces can resolve to the SAME instance per request.
        services.AddScoped<ShortUrlAnalyticsRepository>();
        services.AddScoped<IShortUrlAnalyticsRepository>(sp => sp.GetRequiredService<ShortUrlAnalyticsRepository>());
        services.AddScoped<IShortUrlAnalyticsDapperQueries>(sp => sp.GetRequiredService<ShortUrlAnalyticsRepository>());
 
        // 4. Decorators (Using Scrutor)
        // Decorate the Query side and Query side independently to respect ISP.
        services.Decorate<IShortUrlAnalyticsRepository, CachedShortUrlAnalyticsRepository>();
        services.Decorate<IShortUrlAnalyticsDapperQueries, CachedShortUrlAnalyticsDapperQueries>();
    }
}
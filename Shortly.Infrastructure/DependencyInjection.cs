using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.ClickTracking;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Organizations_legacy.Contracts;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ClickEvents;
using Shortly.Domain.RefreshToken;
using Shortly.Domain.RepositoryContract.Invitations;
using Shortly.Domain.RepositoryContract.Members;
using Shortly.Domain.RepositoryContract.Organizations;
using Shortly.Domain.RepositoryContract.Teams;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;
using Shortly.Infrastructure.Authentication;
using Shortly.Infrastructure.Authorization;
using Shortly.Infrastructure.ClickTracking;
using Shortly.Infrastructure.Clock;
using Shortly.Infrastructure.Data;
using Shortly.Infrastructure.Email;
using Shortly.Infrastructure.Repositories;
using Shortly.Infrastructure.Repositories.Invetations;
using Shortly.Infrastructure.Repositories.Members;
using Shortly.Infrastructure.Repositories.Organizations;
using Shortly.Infrastructure.Repositories.Teams;
using Shortly.Infrastructure.Repositories.Users;
using Shortly.Infrastructure.ScheduledJobs;

namespace Shortly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        
        AddPersistence(services, configuration);
        AddRepositories(services);
        AddAuthentication(services, configuration);
        AddAuthorization(services);
        AddEmail(services, configuration);
        AddBackgroundJobs(services);
        AddClickTracking(services, configuration);

        return services;
    }
    
    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Database") ??
            throw new InvalidOperationException("Connection string 'Database' was not found.");
        
        services.AddDbContextFactory<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });
 
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
    }

    private static void AddRepositories(IServiceCollection services)
    {
        // User Management
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserUsageRepository, UserUsageRepository>();
 
        // Tokens
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserActionTokenRepository, UserActionTokenRepository>();
 
        // URL Management
        services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
        
        // Click Tracking
        services.AddScoped<IClickEventRepository, ClickEventRepository>();
 
        // Organization Management
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationUsageRepository, OrganizationUsageRepository>();
        services.AddScoped<IOrganizationUsageQeries, OrganizationUsageRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IOrganizationTeamRepository, OrganizationTeamRepository>();
        services.AddScoped<IOrganizationTeamMemberRepository, OrganizationTeamMemberRepository>();
        services.AddScoped<IOrganizationInvitationRepository, OrganizationInvitationRepository>();
    }
    
    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "Shortly.OAuth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            configuration["Jwt:Key"]
                            ?? throw new ConfigurationException("JWT signing key is missing in configuration."))),
                    ClockSkew = TimeSpan.Zero
                })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]
                                   ?? throw new ConfigurationException("Google ClientId is missing in configuration.");
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]
                                       ?? throw new ConfigurationException(
                                           "Google ClientSecret is missing in configuration.");

                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.SaveTokens = true;
                options.ClaimActions.MapJsonKey("picture", "picture");
            });
    }

    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
    }
    
    private static void AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddSingleton<EmailQueueService>();
    
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        services.AddScoped<IEmailTemplates, EmailTemplates>();
        services.AddScoped<IEmailNotifier, EmailNotifier>();

        services.AddHostedService<EmailBackgroundWorker>();
    }

    private static void AddBackgroundJobs(IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            // var usageResetKey = new JobKey("MonthlyUsageResetJob", "UsageResetGroup");
            // q.AddJob<MonthlyUsageResetJob>(opts => opts.WithIdentity(usageResetKey).StoreDurably());
            // q.AddTrigger(opts => opts
            //     .ForJob(usageResetKey)
            //     .WithIdentity("MonthlyUsageResetTrigger", "UsageResetGroup")
            //     .WithCronSchedule(
            //         "0 0 0 1 * ?",
            //         x => x
            //             .InTimeZone(TimeZoneInfo.Utc)
            //             .WithMisfireHandlingInstructionFireAndProceed()));

            var tokenCleanupKey = new JobKey("ExpiredTokenCleanupJob", "CleanupGroup");
            q.AddJob<ExpiredTokenCleanupJob>(opts => opts.WithIdentity(tokenCleanupKey).StoreDurably());
            q.AddTrigger(opts => opts
                .ForJob(tokenCleanupKey)
                .WithIdentity("ExpiredTokenCleanupTrigger", "CleanupGroup")
                .WithCronSchedule(
                    "0 0 3 * * ?",
                    x => x
                        .InTimeZone(TimeZoneInfo.Utc)
                        .WithMisfireHandlingInstructionFireAndProceed()));
        });

        services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
    }

    private static void AddClickTracking(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IClickEventQueue, ClickEventQueue>();
        services.AddHostedService<ClickTrackingBackgroundService>();
        services.AddSingleton<IUserAgentParser, UserAgentParser>();
        services.AddScoped<IClickEventRepository, ClickEventRepository>();

        services.AddOptions<GeoLite2Options>()
            .Bind(configuration.GetSection(GeoLite2Options.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.DatabasePath), "GeoLite2:DatabasePath is required.")
            .Validate(o => o.UpdateIntervalDays > 0, "GeoLite2:UpdateIntervalDays must be positive.")
            .ValidateOnStart();

        services.AddHttpClient("MaxMind", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddSingleton<MaxMindGeoLocationService>();
        services.AddSingleton<IGeoLocationService>(sp => sp.GetRequiredService<MaxMindGeoLocationService>());
        services.AddHostedService<GeoDatabaseUpdaterService>();
    }
}
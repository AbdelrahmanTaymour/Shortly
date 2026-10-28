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
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.ClickTracking.Contracts;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Organizations_legacy.Contracts;
using Shortly.Core.Users_legacy.Contracts;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.ClickTracking;
using Shortly.Domain.RepositoryContract.Invitations;
using Shortly.Domain.RepositoryContract.Members;
using Shortly.Domain.RepositoryContract.Organizations;
using Shortly.Domain.RepositoryContract.Profile;
using Shortly.Domain.RepositoryContract.Security;
using Shortly.Domain.RepositoryContract.Teams;
using Shortly.Domain.RepositoryContract.Tokens;
using Shortly.Domain.RepositoryContract.UserAuditLog;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.ShortUrls;
using Shortly.Infrastructure.Authentication;
using Shortly.Infrastructure.Authorization;
using Shortly.Infrastructure.BackgroundServices;
using Shortly.Infrastructure.Clock;
using Shortly.Infrastructure.Data;
using Shortly.Infrastructure.Email;
using Shortly.Infrastructure.Repositories;
using Shortly.Infrastructure.Repositories.Tokens;
using Shortly.Infrastructure.Repositories.ClickTracking;
using Shortly.Infrastructure.Repositories.Invetations;
using Shortly.Infrastructure.Repositories.Members;
using Shortly.Infrastructure.Repositories.Organizations;
using Shortly.Infrastructure.Repositories.Profile;
using Shortly.Infrastructure.Repositories.Security;
using Shortly.Infrastructure.Repositories.Teams;
using Shortly.Infrastructure.Repositories.UserAuditLog;
using Shortly.Infrastructure.Repositories.Users;
using Shortly.Infrastructure.ScheduledJobs;
using Shortly.Infrastructure.Services.GeoLocation;
using AuthenticationService = Shortly.Infrastructure.Authentication.AuthenticationService;
using IAuthenticationService = Shortly.Core.Abstractions.Authentication.IAuthenticationService;

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
        AddGeoLocation(services);

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
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserSecurityRepository, UserSecurityRepository>();
        services.AddScoped<IUserUsageRepository, UserUsageRepository>();
        services.AddScoped<IUserAuditLogRepository, UserAuditLogRepository>();
        services.AddScoped<IUserQueries, UserQueries>();
 
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
        
        services.AddScoped<IAuthenticationService, AuthenticationService>();
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
            q.UseMicrosoftDependencyInjectionJobFactory();
            q.ScheduleJob<MonthlyUsageResetJob>(trigger => trigger
                .WithIdentity("MonthlyUsageResetTrigger")
                .StartNow()
                .WithCronSchedule("0 0 1 * * ?"));
        });
        services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
        services.AddScoped<MonthlyUsageResetJob>();

        services.AddHostedService<ClickTrackingBackgroundWorker>();
    }

    private static void AddGeoLocation(IServiceCollection services)
    {
        services.AddHttpClient<IGeoLocationService, GeoLocationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "Short.ly/1.0");
        });
    }
}
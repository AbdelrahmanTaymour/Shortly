using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.RepositoryContract;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Infrastructure.DbContexts;
using Shortly.Infrastructure.Repositories;
using Shortly.Infrastructure.Repositories.OrganizationManagement;
using Shortly.Infrastructure.Repositories.UserManagement;

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
    /// <returns>
    /// The same <see cref="IConfiguration"/> instance, allowing for fluent chaining of service registrations.
    /// </returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        // Register infrastructure-related services.
        services.AddDbContext<SQLServerDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ConnectionString")));

        // User Management
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserSecurityRepository, UserSecurityRepository>();
        services.AddScoped<IUserUsageRepository, UserUsageRepository>();
        services.AddScoped<IUserAuditLogRepository, UserAuditLogRepository>();
        services.AddScoped<IUserAdministrationRepository, UserAdministrationRepository>();
        
        // Organization Management
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        
        services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        
        return services;
    }
}
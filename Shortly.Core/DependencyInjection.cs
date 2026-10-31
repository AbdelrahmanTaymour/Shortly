using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Abstractions.Behaviors;
using Shortly.Core.Common;
using Shortly.Core.Invitations.Contracts;
using Shortly.Core.Invitations.Services;
using Shortly.Core.Teams.Contracts;
using Shortly.Core.Teams.Services;
using Shortly.Domain.ShortUrls;

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
        
        services.AddTransient<ShortCodeGenerator>();

        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        
        // Organization Management (legacy, not yet migrated to CQRS)
        services.AddScoped<IOrganizationTeamService, OrganizationTeamService>();
        services.AddScoped<IOrganizationInvitationService, OrganizationInvitationService>();

        return services;
    }
}
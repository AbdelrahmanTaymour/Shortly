using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Shortly.Api.Extensions;
using Shortly.Api.Middleware;
using Shortly.Api.Routing;
using Shortly.Core;
using Shortly.Infrastructure;

namespace Shortly.Api;

/// <summary>
/// Represents the entry point for the Shortly API application.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure controllers, custom routing conventions, and global JSON enum serialization
        builder.Services.AddControllers(options => 
            options.Conventions.Add(new OrganizationScopedRoutesConvention()))
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddFluentValidationAutoValidation();

        // Configure Swagger/OpenAPI documentation with XML comments and JWT Bearer security
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Shortly API",
                Version = "v1",
                Description = "API documentation for the Link Management System"
            });

            // Include XML documentation comments from API, Core, and Domain assemblies
            foreach (var xml in new[] { $"{Assembly.GetExecutingAssembly().GetName().Name}.xml", "Shortly.Core.xml", "Shortly.Domain.xml" })
            {
                var fullPath = Path.Combine(AppContext.BaseDirectory, xml);
                if (File.Exists(fullPath)) options.IncludeXmlComments(fullPath);
            }

            // Register JWT security definition for Swagger UI authorization
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        },
                        Scheme = "Oauth2",
                        Name = JwtBearerDefaults.AuthenticationScheme,
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });
        });

        // Configure CORS policies for frontend clients and local development environments
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(
                        "http://localhost:3000", "https://localhost:3000",
                        "http://127.0.0.1:5500", "http://127.0.0.1:5501", 
                        "http://127.0.0.1:5502", "http://127.0.0.1:5503")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });

            options.AddPolicy("FrontendPolicy", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "https://shortly-frontend-tau.vercel.app")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // Register custom edge protection, infrastructure, and core business services
        builder.Services.AddEdgeProtection(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddCore(builder.Configuration);

        var app = builder.Build();

        // ── Middleware Pipeline ───────────────────────────────────────────────

        // Must run first to ensure client IP and scheme correctly reflect proxy headers
        app.UseForwardedHeaders();

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseHttpsRedirection();

        // Enable Swagger UI only in development environments
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.UseCors("FrontendPolicy");

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        // Automatically apply pending EF Core migrations on startup if configured or in development
        if (app.Configuration.GetValue<bool?>("Database:MigrateOnStartup") ?? app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
        }

        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
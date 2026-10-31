using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Shortly.API.Middleware;
using Shortly.Core;
using Shortly.Infrastructure;

namespace Shortly.API;

/// <summary>
/// Represents the entry point for the Shortly API application.
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point for the Shortly.API application.
    /// Initializes the web application, configures services, and sets up the middleware pipeline.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Basic Services
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        builder.Services.AddEndpointsApiExplorer();

        // Validation
        builder.Services.AddFluentValidationAutoValidation();

        // ── Swagger / OpenAPI ─────────────────────────────────────────────────
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Shortly API",
                Version = "v1",
                Description = "API documentation for the Link Management System"
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);

            foreach (var xml in new[] { "Shortly.Core.xml", "Shortly.Domain.xml" })
            {
                var fullPath = Path.Combine(AppContext.BaseDirectory, xml);
                if (File.Exists(fullPath)) options.IncludeXmlComments(fullPath);
            }

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

        // ── CORS ──────────────────────────────────────────────────────────────
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:3000",
                        "https://localhost:3000",
                        "http://127.0.0.1:5500",
                        "http://127.0.0.1:5501",
                        "http://127.0.0.1:5502",
                        "http://127.0.0.1:5503")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            options.AddPolicy("FrontendPolicy", policy =>
            {
                policy
                    .WithOrigins("http://localhost:3000", "https://localhost:3000", "https://shortly-frontend-tau.vercel.app")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // ── Infrastructure & Core Registrations ───────────────────────────────
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddCore(builder.Configuration);

        var app = builder.Build();

        // ── Middleware Pipeline ───────────────────────────────────────────────
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseHttpsRedirection();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();

        app.UseCors("FrontendPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        // Apply pending EF Core migrations (PostgreSQL ApplicationDbContext)
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
        }

        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
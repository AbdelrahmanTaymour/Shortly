using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using Shortly.Core;
using Shortly.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Shortly.API.Authentication;
using Shortly.API.Authorization;
using Shortly.API.Middleware;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.ServiceContracts.Authentication;

namespace Shortly.API;

public class Program
{
    [Obsolete("Obsolete")]
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Basic Services
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        builder.Services.AddEndpointsApiExplorer();

        // Logging Configuration
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            options.IncludeScopes = false;
        });

        // FluentValidations
        builder.Services.AddFluentValidationAutoValidation();

        // Swagger/OpenAPI Configuration
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Shortly API",
                Version = "v1",
                Description = "API documentation for the Link Management System"
            });
            
            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // XML Comments for other referenced projects
            var additionalXmlFiles = new[] { "Shortly.Core.xml", "Shortly.Domain.xml" };
            foreach (var xml in additionalXmlFiles)
            {
                var fullPath = Path.Combine(AppContext.BaseDirectory, xml);
                if (File.Exists(fullPath))
                {
                    options.IncludeXmlComments(fullPath);
                }
            }

            // JWT Authentication for Swagger
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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

        // Add CORS services
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policyBuilder =>
            {
                policyBuilder.WithOrigins("http://localhost:3000", "https://localhost:3000", 
                        "http://127.0.0.1:5501", "https://127.0.0.1:5501") // Add your frontend URLs
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); // Only if you need cookies/sessions
            });
        });


        // Authentication
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] 
                                                                        ?? throw new ConfigurationException("JWT signing key is missing in configuration."))),
                    ClockSkew = TimeSpan.Zero
                });


        // Authorization & Authentication
        builder.Services.AddUrlShortenerAuthorization();
        builder.Services.AddScoped<IAuthenticationContextProvider, AuthenticationContextProvider>();

        
        // Custom Services
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddCore(builder.Configuration);

        var app = builder.Build();

        // ===== MIDDLEWARE PIPELINE (ORDER IS CRITICAL) =====

        // 1. Exception Handling
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // 2. CORS
        app.UseCors();

        // 3. Development-only middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // 4. HTTPS Redirection
        app.UseHttpsRedirection();

        // 5. Routing
        app.UseRouting();
        
        // 6. Authentication
        app.UseAuthentication();

        // 7. Authorization
        app.UseAuthorization();

        // 8. Controller mapping
        app.MapControllers();

        app.Run();
    }
}
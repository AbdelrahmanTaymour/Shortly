using System.Text;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using Shortly.Core;
using Shortly.Core.Mappers;
using Shortly.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Shortly.API.Authorization;
using Shortly.API.Middleware;

namespace Shortly.API;

public class Program
{
    [Obsolete("Obsolete")]
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Basic Services
        builder.Services.AddControllers().AddJsonOptions(options => {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            });
        builder.Services.AddEndpointsApiExplorer();
        
        // Memory Cache for performance optimization
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1024; // Maximum number of cache entries
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });
        
        // Logging Configuration with enhanced performance
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            options.IncludeScopes = false;
        });
        
        // Add structured logging for better performance analysis
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        });
        
        // FluentValidations
        builder.Services.AddFluentValidationAutoValidation();
        
        // Swagger/OpenAPI Configuration with enhanced documentation
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Shortly API - Link Management System", 
                Version = "v1.0.0",
                Description = "High-performance API for URL shortening and link management with comprehensive error handling and performance monitoring.",
                Contact = new OpenApiContact
                {
                    Name = "Shortly Team",
                    Email = "support@shortly.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });
            
            // JWT Authentication for Swagger
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
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

            // Add operation filters for better documentation
            options.EnableAnnotations();
            
            // Include XML comments for better documentation
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
        
        // CORS Configuration with enhanced security
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://127.0.0.1:5501", "http://localhost:3000", "https://shortly.com")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders("X-Trace-Id", "X-Error-Code", "X-Cache", "X-Cache-Timestamp", "X-Response-Time");
            });
        });

        // Authentication with enhanced security
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
                    ClockSkew = TimeSpan.Zero
                };
                
                // Enhanced JWT events for better logging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogDebug("JWT token validated successfully for user: {User}", 
                            context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    }
                };
            });

        // Authorization
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddUrlShortenerAuthorization();
        
        // Custom Services
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddCore();

        var app = builder.Build();

        // ===== OPTIMIZED MIDDLEWARE PIPELINE (ORDER IS CRITICAL) =====
        
        // 1. Performance Monitoring (first to capture all requests)
        app.UseMiddleware<PerformanceMonitoringMiddleware>();

        // 2. Exception Handling (early to catch all exceptions)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // 3. CORS (before routing)
        app.UseCors("AllowFrontend");
        
        // 4. Response Caching (after CORS, before routing)
        app.UseMiddleware<ResponseCachingMiddleware>();
        
        // 5. Development-only middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Shortly API v1");
                options.RoutePrefix = "api-docs";
                options.DocumentTitle = "Shortly API Documentation";
                options.DefaultModelsExpandDepth(2);
                options.DefaultModelExpandDepth(2);
            });
        }

        // 6. HTTPS Redirection (in production)
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        
        // 7. Static Files (if any)
        app.UseStaticFiles();
        
        // 8. Routing
        app.UseRouting();
        
        // 9. Authentication
        app.UseAuthentication();
        
        // 10. Authorization
        app.UseAuthorization();
        
        // 11. Response Transformation (after authorization, before controllers)
        app.UseMiddleware<ResponseTransformationMiddleware>();
        
        // 12. Controller mapping
        app.MapControllers();
        
        // 13. Health check endpoint
        app.MapGet("/health", () => new { 
            Status = "Healthy", 
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = app.Environment.EnvironmentName
        });
        
        app.Run();
    }
}

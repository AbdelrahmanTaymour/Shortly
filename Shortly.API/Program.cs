using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shortly.API.Authentication;
using Shortly.API.Authorization;
using Shortly.API.Middleware;
using Shortly.Core;
using Shortly.Core.Auth.Contracts;
using Shortly.Core.Common;
using Shortly.Core.Common.Abstractions;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Infrastructure;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.API;

/// <summary>
/// Represents the entry point for the Shortly. API application.
/// </summary>
public class Program
{
    /// <summary>
    /// The entry point for the application.
    /// </summary>
    /// <param name="args">An array of command-line arguments passed to the application.</param>
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

        // Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            options.IncludeScopes = false;
        });

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

            // XML comments from the API project
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);

            // XML comments from referenced projects
            foreach (var xml in new[] { "Shortly.Core.xml", "Shortly.Domain.xml" })
            {
                var fullPath = Path.Combine(AppContext.BaseDirectory, xml);
                if (File.Exists(fullPath)) options.IncludeXmlComments(fullPath);
            }

            // JWT bearer definition for the Swagger UI
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
        //
        // AllowCredentials() is required for the browser to send the HttpOnly
        // refreshToken cookie on cross-origin fetch requests (credentials:'include').
        // WithOrigins() must list exact origins — a wildcard ("*") is incompatible
        // with AllowCredentials() and will be rejected by every modern browser.
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
                    .WithOrigins("http://localhost:3000", "https://localhost:3000")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // ── Authentication ────────────────────────────────────────────────────
        //
        // Three schemes are registered:
        //
        //   1. JwtBearer  — validates the short-lived access token sent in the
        //                   Authorization: Bearer header on every API call.
        //                   This is the default authenticate / challenge scheme.
        //
        //   2. Cookie     — used ONLY as a transient sign-in scheme during the
        //                   Google OAuth dance (Google → callback). The cookie
        //                   is named "Shortly.OAuth", lives for 5 minutes, and
        //                   is deleted once the callback method completes.
        //                   It is NOT the session/refresh cookie.
        //
        //   3. Google     — handles the redirect to Google and the code exchange.
        //                   SignInScheme points to the Cookie scheme above.
        //
        // The long-lived refreshToken cookie used for session management is set
        // manually in AuthController and OAuthController via CookieOptions —
        // it is not managed by any authentication scheme.
        builder.Services.AddAuthentication(options =>
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
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
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
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]
                            ?? throw new ConfigurationException("JWT signing key is missing in configuration."))),
                    ClockSkew = TimeSpan.Zero
                })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
                                   ?? throw new ConfigurationException("Google ClientId is missing in configuration.");
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
                                       ?? throw new ConfigurationException(
                                           "Google ClientSecret is missing in configuration.");

                options.Scope.Add("profile");
                options.Scope.Add("email");

                // Use the Cookie scheme registered above as the transient sign-in store.
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // Save tokens in case they are needed server-side in the future.
                options.SaveTokens = true;

                // Map the "picture" claim from Google's ID token.
                options.ClaimActions.MapJsonKey("picture", "picture");
            });

        // Authorization
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContext, UserContext>();
        builder.Services.AddUrlShortenerAuthorization();
        builder.Services.AddScoped<IAuthenticationContextProvider, AuthenticationContextProvider>();

        // Domain Services
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddCore(builder.Configuration);

        // ═════════════════════════════════════════════════════════════════════
        // MIDDLEWARE PIPELINE
        //
        // ORDER IS CRITICAL in ASP.NET Core. The rules are:
        //   • UseHttpsRedirection  — before everything else (transport layer)
        //   • UseRouting           — before UseCors and UseAuthentication
        //   • UseCors              — after UseRouting, BEFORE UseAuthentication
        //   • UseAuthentication    — before UseAuthorization
        //   • UseAuthorization     — last auth middleware
        //
        // References:
        //   https://learn.microsoft.com/aspnet/core/fundamentals/middleware/
        // ═════════════════════════════════════════════════════════════════════
        var app = builder.Build();

        // Global exception handler
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Redirect HTTP → HTTPS at the transport level before any routing.
        app.UseHttpsRedirection();

        // Dev-only diagnostics.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Route matching — must precede CORS and auth.
        app.UseRouting();

        // CORS — must come after UseRouting, so the route is known,
        //    and before UseAuthentication so preflight OPTIONS requests
        //    are handled before the JWT validator runs.
        app.UseCors("FrontendPolicy");

        // Authentication — validates the Bearer token / cookie on every request.
        app.UseAuthentication();

        // Authorization — evaluates [Authorize] policies.
        app.UseAuthorization();

        // Apply pending EF Core migrations (Docker / CI entrypoint).
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SqlServerDbContext>();
            context.Database.Migrate();
        }

        // Map controllers and SPA fallback.
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
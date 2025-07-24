using System.Security.Authentication;
using FluentValidation;
using Shortly.Core.DTOs.ValidationDTOs;

namespace Shortly.API.Middleware;

/// <summary>
/// Middleware that handles unhandled exceptions occurring during the HTTP request pipeline execution.
/// </summary>
/// <remarks>
/// This middleware catches any unhandled exceptions thrown by downstream middleware or controllers,
/// logs the exception details using the configured <see cref="ILogger"/>, and returns a standardized
/// 500 Internal Server Error response to the client.
/// </remarks>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    /// <summary>
    /// Processes an HTTP request and handles any unhandled exceptions thrown during its execution.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method delegates the request to the next middleware in the pipeline using <c>_next</c>.
    /// If an exception is thrown by any downstream component, it catches the exception, logs the error details,
    /// sets the response status code to 500 (Internal Server Error), and returns a JSON response containing
    /// the exception message and type. If an inner exception is present, its details are also logged.
    /// </remarks>
    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning("Authentication failed: {Message}", ex.Message);
            
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new ExceptionResponseDto
            (
                Message: ex.Message,
                Type: ex.GetType().ToString()
            ));
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed: {Message}", ex.Message);

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new ExceptionResponseDto
            (
                Message: ex.Message,
                Type: ex.GetType().ToString()
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.GetType().ToString()}: {ex.Message}");

            if (ex.InnerException is not null)
            {
                _logger.LogError($"{ex.InnerException.GetType().ToString()}: {ex.InnerException.Message}");
            }
            
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(new ExceptionResponseDto
            (
                Message: ex.Message,
                Type: ex.GetType().ToString()
            ));

        }
    }
}

/// <summary>
/// Extension methods for registering <see cref="ExceptionHandlingMiddleware"/> in the application's HTTP request pipeline.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="ExceptionHandlingMiddleware"/> to the application's request pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> instance, to allow chaining.</returns>
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}


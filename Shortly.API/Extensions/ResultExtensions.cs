using Microsoft.AspNetCore.Mvc;
using Shortly.Domain.Abstractions;

namespace Shortly.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a successful result to a problem.");

        var statusCode = GetStatusCode(result.Error.Type);

        var problemDetails = new ProblemDetails
        {
            Type = GetType(result.Error.Type),
            Title = GetTitle(result.Error.Type),
            Detail = result.Error.Message,
            Status = statusCode
        };

        problemDetails.Extensions["errorCode"] = result.Error.Code;

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    private static int GetStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status400BadRequest
    };

    private static string GetTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.Validation => "Validation error",
        _ => "Bad request"
    };

    private static string GetType(ErrorType errorType) => errorType switch
    {
        ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
        ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.5.1"
    };
}
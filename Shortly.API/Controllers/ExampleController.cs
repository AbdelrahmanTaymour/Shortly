using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Shortly.API.Controllers;

/// <summary>
///     Example controller demonstrating minimal, clean controller implementation.
///     All response formatting, error handling, and performance monitoring is centralized in middleware.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Example Operations", Description = "Example endpoints demonstrating clean controller implementation")]
public class ExampleController : ControllerBase
{
    /// <summary>
    ///     Simple GET endpoint - returns data directly.
    ///     Response transformation middleware will automatically wrap it in standardized format.
    /// </summary>
    /// <returns>Simple data object</returns>
    [HttpGet("simple")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Simple GET", Description = "Returns simple data")]
    public IActionResult GetSimple()
    {
        // Just return the data - middleware handles everything else
        return Ok(new { message = "Hello from clean controller!", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    ///     POST endpoint with validation - throws exceptions naturally.
    ///     Exception handling middleware will catch and format errors.
    /// </summary>
    /// <param name="data">Input data</param>
    /// <returns>Processed data</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Process data", Description = "Processes input data")]
    public IActionResult ProcessData([FromBody] object data)
    {
        // Natural exception handling - middleware will catch and format
        if (data == null)
            throw new ArgumentNullException(nameof(data), "Data cannot be null");

        // Simulate processing
        var result = new { 
            processed = true, 
            originalData = data, 
            processedAt = DateTime.UtcNow 
        };

        return Ok(result);
    }

    /// <summary>
    ///     Endpoint that returns different status codes.
    ///     Response transformation middleware handles all status codes appropriately.
    /// </summary>
    /// <param name="status">Desired status code</param>
    /// <returns>Response with specified status</returns>
    [HttpGet("status/{status:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Test status codes", Description = "Returns response with specified status code")]
    public IActionResult TestStatus(int status)
    {
        return status switch
        {
            200 => Ok(new { message = "Success!", status = 200 }),
            400 => BadRequest(new { error = "Bad request", status = 400 }),
            404 => NotFound(new { error = "Not found", status = 404 }),
            _ => StatusCode(status, new { error = "Custom status", status })
        };
    }

    /// <summary>
    ///     Async endpoint demonstrating clean async/await pattern.
    ///     Performance monitoring middleware automatically tracks execution time.
    /// </summary>
    /// <param name="delay">Delay in milliseconds</param>
    /// <returns>Async result</returns>
    [HttpGet("async/{delay:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Async operation", Description = "Simulates async operation with delay")]
    public async Task<IActionResult> AsyncOperation(int delay = 1000)
    {
        // Simulate async work
        await Task.Delay(delay);

        return Ok(new { 
            message = "Async operation completed", 
            delay = delay,
            completedAt = DateTime.UtcNow 
        });
    }

    /// <summary>
    ///     Endpoint that throws different types of exceptions.
    ///     Exception handling middleware will catch and format appropriately.
    /// </summary>
    /// <param name="exceptionType">Type of exception to throw</param>
    /// <returns>Never returns due to exception</returns>
    [HttpGet("exception/{exceptionType}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(Summary = "Test exceptions", Description = "Throws specified exception type")]
    public IActionResult TestException(string exceptionType)
    {
        throw exceptionType.ToLower() switch
        {
            "argument" => new ArgumentException("Invalid argument provided"),
            "null" => new ArgumentNullException("parameter", "Parameter cannot be null"),
            "unauthorized" => new UnauthorizedAccessException("Access denied"),
            "timeout" => new TimeoutException("Operation timed out"),
            "custom" => new InvalidOperationException("Custom business rule violation"),
            _ => new Exception("Unknown exception type")
        };
    }
}
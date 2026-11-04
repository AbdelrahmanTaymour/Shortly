using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Domain.Users;
using Shortly.Infrastructure.Authorization;

namespace Shortly.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/diagnostics")]
[Produces("application/json")]
[Authorize]
public class AdminDiagnosticsController : ControllerBase
{
    /// <summary>
    /// Returns connection and header details to verify reverse proxy and hosting setup.
    /// </summary>
    [HttpGet("connection", Name = "AdminDiagnosticsConnection")]
    [RequiredPermission(enPermissions.SystemAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Connection() => Ok(new
    {
        // The final client IP address seen by the app (used for rate limiting)
        remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        scheme = Request.Scheme,
        isHttps = Request.IsHttps,
        host = Request.Host.ToString(),
        
        // Raw proxy headers received by the server
        xForwardedFor = Request.Headers["X-Forwarded-For"].ToString(),
        xForwardedProto = Request.Headers["X-Forwarded-Proto"].ToString(),
        xOriginalFor = Request.Headers["X-Original-For"].ToString(),
        xOriginalProto = Request.Headers["X-Original-Proto"].ToString()
    });
}
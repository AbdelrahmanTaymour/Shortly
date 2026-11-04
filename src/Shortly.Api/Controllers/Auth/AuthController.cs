using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shortly.Api.Extensions;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.Login;
using Shortly.Core.Account.Logout;
using Shortly.Core.Account.LogoutAll;
using Shortly.Core.Account.RefreshAccessToken;
using Shortly.Core.Account.Register;
using Shortly.Core.Exceptions;
using Shortly.Infrastructure.EdgeProtection.Models;

namespace Shortly.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Registers a new user account and returns authentication tokens.</summary>
    [HttpPost("register", Name = "Auth_Register")]
    [AllowAnonymous]
    [EnableRateLimiting(RegistrationRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RegisterCommand(request.Email, request.Username, request.Password);
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    /// <summary>Authenticates user credentials and returns tokens.</summary>
    [HttpPost("login", Name = "Auth_Login")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    /// <summary>Refreshes an expired access token using the secure refresh token cookie.</summary>
    [HttpPost("refresh-token", Name = "Auth_RefreshToken")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RefreshAccessTokenCommand(), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    /// <summary>Signs out of the current device by revoking the refresh token cookie.</summary>
    [HttpPost("logout", Name = "Auth_Logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new LogoutCommand(), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : NoContent();
    }

    /// <summary>Signs out of all devices by revoking all refresh tokens issued to the user.</summary>
    [HttpPost("logout-all", Name = "Auth_LogoutAll")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new LogoutAllCommand(), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : NoContent();
    }
}
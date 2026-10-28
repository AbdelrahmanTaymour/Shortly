using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.Login;
using Shortly.Core.Account.Logout;
using Shortly.Core.Account.LogoutAll;
using Shortly.Core.Account.RefreshAccessToken;
using Shortly.Core.Account.Register;
using Shortly.Core.Exceptions.DTOs;

namespace Shortly.API.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register", Name = "Auth_Register")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
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

    [HttpPost("login", Name = "Auth_Login")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
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

    [HttpPost("refresh-token", Name = "Auth_RefreshToken")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RefreshAccessTokenCommand(), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok(result.Value);
    }

    [HttpPost("logout", Name = "Auth_Logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new LogoutCommand(), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok();
    }

    [HttpPost("logout-all", Name = "Auth_LogoutAll")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new LogoutAllCommand(), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Ok();
    }
}
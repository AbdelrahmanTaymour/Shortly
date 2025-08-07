using MethodTimer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.ServiceContracts.Authentication;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    private readonly IAuthenticationService _authenticationService = authenticationService;

    [Time]
    [HttpPost("register", Name = "Register")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        AuthenticationResponse? authResponse = await _authenticationService.Register(request, cancellationToken);
        if (authResponse is null || !authResponse.Success) return BadRequest();
        return Ok(authResponse);
    }

    [Time]
    [HttpPost("login", Name = "Login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        AuthenticationResponse? authResponse = await _authenticationService.Login(request, cancellationToken);
        if (authResponse is null || !authResponse.Success) return BadRequest();
        return Ok(authResponse);
    }
}
using System.Diagnostics;
using MethodTimer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.ServiceContracts;
using Swashbuckle.AspNetCore.Annotations;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    private readonly IAuthenticationService _authenticationService = authenticationService;
    
    [Time]
    [HttpPost("register", Name = "Register")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto),StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        AuthenticationResponse? authResponse = await _authenticationService.Register(request);
        if (authResponse is null || !authResponse.Success)
        {
            return BadRequest();
        }
        return Ok(authResponse);
    }

    [Time]
    [HttpPost("login", Name = "Login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto),StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        AuthenticationResponse? authResponse = await _authenticationService.Login(request);
        if (authResponse is null || !authResponse.Success)
        {
            return BadRequest();
        }
        return Ok(authResponse);
    }
    
    [Time]
    [HttpPost("refresh-token",Name = "RefreshToken")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto),StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? refreshTokenRequest)
    {
        if (refreshTokenRequest == null || string.IsNullOrEmpty(refreshTokenRequest.RefreshToken))
        {
            return BadRequest("Invalid refresh token request");
        }

        var response = await _authenticationService.RefreshTokenAsync(refreshTokenRequest.RefreshToken);
        if (response == null)
        {
            return Unauthorized("Invalid or expired refresh token");
        }

        return Ok(response);
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Core.DTOs;
using Shortly.Core.ServiceContracts;
using Swashbuckle.AspNetCore.Annotations;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUsersService usersService, IJwtService jwtService) : ControllerBase
{
    private readonly IUsersService _usersService = usersService;
    private readonly IJwtService _jwtService = jwtService;

    
    [HttpPost("register", Name = "Register")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Register user",
        Description = "Registers a new user and returns access and refresh tokens"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Registration successful", typeof(AuthenticationResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid input or registration failed")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        AuthenticationResponse? authResponse = await _usersService.Register(request);
        if (authResponse is null || !authResponse.Success)
        {
            return BadRequest();
        }
        return Ok(authResponse);
    }


    [HttpPost("login", Name = "Login")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Authenticate user",
        Description = "Authenticates a user and returns access and refresh tokens"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Authentication successful", typeof(AuthenticationResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid credentials")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        AuthenticationResponse? authResponse = await _usersService.Login(request);
        if (authResponse is null || !authResponse.Success)
        {
            return BadRequest();
        }
        return Ok(authResponse);
    }
    
    [HttpPost("refresh-token",Name = "RefreshToken")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Refresh JWT tokens",
        Description = "Generates new access and refresh tokens using an existing refresh token"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Token refreshed successfully", typeof(TokenResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Missing or invalid refresh token")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Refresh token is expired or invalid")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? refreshTokenRequest)
    {
        if (refreshTokenRequest == null || string.IsNullOrEmpty(refreshTokenRequest.RefreshToken))
        {
            return BadRequest("Invalid refresh token request");
        }

        var response = await _jwtService.RefreshTokenAsync(refreshTokenRequest.RefreshToken, extendExpiry: false);
        if (response == null)
        {
            return Unauthorized("Invalid or expired refresh token");
        }

        return Ok(response);
    }
}

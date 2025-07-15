using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Core.DTOs;
using Shortly.Core.ServiceContracts;
using Swashbuckle.AspNetCore.Annotations;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUsersService usersService) : ControllerBase
{
    private readonly IUsersService _usersService = usersService;
    
    [HttpPost("register")]
    
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        AuthenticationResponse? authResponse = await _usersService.Register(request);
        if (authResponse is null || !authResponse.Success)
        {
            return BadRequest();
        }
        return Ok(authResponse);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Authenticate user",
        Description = "Authenticates a user and returns access and refresh tokens"
    )]
    [SwaggerResponse(200, "Authentication successful")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid credentials")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        AuthenticationResponse? authResponse =  await _usersService.Login(request);
        if (authResponse is null || !authResponse.Success)
        {
            return BadRequest();
        }
        return Ok(authResponse);
    }

    // TODO: REFRESH TOKEN END-POINTS
}
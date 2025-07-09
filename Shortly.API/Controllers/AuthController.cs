using Microsoft.AspNetCore.Mvc;
using Shortly.Core.DTOs;
using Shortly.Core.ServiceContracts;

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
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        AuthenticationResponse? authResponse =  await _usersService.Login(request);
        if (authResponse is null || !authResponse.Success)
        {
            return BadRequest();
        }
        return Ok(authResponse);
    }

}
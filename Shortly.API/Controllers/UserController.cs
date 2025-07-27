using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.ServiceContracts;

namespace Shortly.API.Controllers;

//[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
    
    private string GetCurrentEmail()
    {
        var userEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
        return userEmailClaim ?? string.Empty;
    }
    
    
    // Advanced Query
    [HttpGet("getAllUser", Name = "GetAllUsers")]
    [ProducesResponseType(typeof(List<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }
}
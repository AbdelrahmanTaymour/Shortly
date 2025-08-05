using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.ServiceContracts;

namespace Shortly.API.Controllers;

//[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController() : ControllerBase
{
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

    private long GetCurrentUserPermissions()
    {
        var permissionsClaim = User.FindFirst("Permissions")?.Value;
        return long.TryParse(permissionsClaim, out var permissions) ? permissions : 0;
    }
    
}
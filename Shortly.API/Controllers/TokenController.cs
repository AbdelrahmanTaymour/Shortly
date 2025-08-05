using MethodTimer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.ServiceContracts.Authentication;

namespace Shortly.API.Controllers;

[Route("api/auth/tokens")]
[ApiController]
public class TokenController(ITokenService tokenService) : ControllerBase
{
    [Time]
    [HttpPost("refresh",Name = "RefreshToken")]
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

        var response = await tokenService.RefreshTokenAsync(refreshTokenRequest.RefreshToken);
        if (response == null)
        {
            return Unauthorized("Invalid or expired refresh token");
        }
        return Ok(response);
    }
    
    
}
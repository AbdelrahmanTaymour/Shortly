using MethodTimer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Core.ServiceContracts.Tokens;

namespace Shortly.API.Controllers;

/// <summary>
/// Controller for handling user authentication operations including registration, login, logout, and token management.
/// </summary>
/// <param name="authenticationService">Service for handling authentication operations.</param>
/// <param name="tokenService">Service for managing JWT and refresh tokens.</param>
/// <remarks>
/// This controller provides endpoints for core authentication functionality including:
/// - User registration and login
/// - Token refresh operations
/// - Session management (logout single/all sessions)
/// 
/// All sensitive operations use secure token-based authentication and follow security best practices.
/// </remarks>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthenticationService authenticationService, ITokenService tokenService) : ControllerApiBase
{
    /// <summary>
    /// Registers a new user account with email verification.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Authentication response with user details and tokens upon successful registration.</returns>
    /// <example>POST /api/auth/register</example>
    /// <remarks>
    /// Creates a new user account and automatically sends an email verification link.
    /// The user receives authentication tokens immediately, but email confirmation may be required
    /// for certain features depending on the system configuration.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///         "email": "user@example.com",
    ///         "username": "newuser123",
    ///         "password": "SecurePassword123!"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">User was registered successfully and authentication tokens are provided.</response>
    /// <response code="400">The request data is invalid or malformed.</response>
    /// <response code="500">An internal server error occurred.</response>
    [Time]
    [HttpPost("register", Name = "Register")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        AuthenticationResponse? authResponse = await authenticationService.Register(request, cancellationToken);
        if (authResponse is null || !authResponse.Success) return BadRequest();
        return Ok(authResponse);
    }

    /// <summary>
    /// Authenticates a user with email/username and password credentials.
    /// </summary>
    /// <param name="request">The login request containing credentials.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Authentication response with user details and tokens upon successful login.</returns>
    /// <example>POST /api/auth/login</example>
    /// <remarks>
    /// Validates user credentials and provides JWT access and refresh tokens for authenticated sessions.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/login
    ///     {
    ///         "emailOrUsername": "user@example.com",
    ///         "password": "UserPassword123!"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">User was authenticated successfully and tokens are provided.</response>
    /// <response code="400">The credentials are invalid or the request is malformed.</response>
    /// <response code="500">An internal server error occurred.</response>
    [Time]
    [HttpPost("login", Name = "Login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        AuthenticationResponse? authResponse = await authenticationService.Login(request, cancellationToken);
        if (authResponse is null || !authResponse.Success) return BadRequest();
        return Ok(authResponse);
    }
    
    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="refreshTokenRequest">The request containing the refresh token.</param>
    /// <returns>New access and refresh tokens if the refresh token is valid.</returns>
    /// <example>POST /api/auth/refresh-token</example>
    /// <remarks>
    /// Generates new authentication tokens when the access token has expired, but the refresh token is still valid.
    /// The old refresh token is invalidated, and a new one is issued for security.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/refresh-token
    ///     {
    ///         "refreshToken": "base64_encoded_refresh_token"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">New tokens were generated successfully.</response>
    /// <response code="400">The request is invalid or missing required data.</response>
    /// <response code="401">The refresh token is invalid or expired.</response>
    /// <response code="500">An internal server error occurred.</response>
    [Time]
    [HttpPost("refresh-token", Name = "RefreshToken")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest)
    {
        var response = await tokenService.RefreshTokenAsync(refreshTokenRequest.RefreshToken);
        if (response == null) return Unauthorized("Invalid or expired refresh token");
        return Ok(response);
    }

    /// <summary>
    /// Logs out the current user session by revoking the specified refresh token.
    /// </summary>
    /// <param name="request">The logout request containing the refresh token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Response indicating whether the logout was successful.</returns>
    /// <example>POST /api/auth/logout</example>
    /// <remarks>
    /// <para><strong>Authentication Required:</strong> User must be authenticated to logout.</para>
    /// <para>This endpoint revokes a specific refresh token, effectively logging out the user from the current session/device.
    /// The access token will continue to work until it expires naturally (typically within minutes),
    /// but the refresh token becomes unusable immediately.</para>
    /// 
    /// <para>For complete immediate logout, the client should:</para>
    /// <list type="number">
    ///   <item><description>Call this endpoint to revoke the refresh token</description></item>
    ///   <item><description>Remove both access and refresh tokens from client storage</description></item>
    ///   <item><description>Redirect user to login page</description></item>
    /// </list>
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/logout
    ///     {
    ///         "refreshToken": "base64_encoded_refresh_token"
    ///     }
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "success": true,
    ///         "message": "Logged out successfully."
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">User was logged out successfully.</response>
    /// <response code="400">The request is invalid or missing required data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">The specified refresh token was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("logout", Name = "Logout")]
    [Authorize]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var success = await tokenService.RevokeTokenAsync(request.RefreshToken, cancellationToken);
        
        var response = new LogoutResponse
        (
            Success: success,
            Message: success ? "Logged out successfully." : "Failed to logout. Token may already be revoked."
        );

        return Ok(response);
    }
    
    /// <summary>
    /// Ensures the token doesn't contain whitespace characters that could indicate tampering or corruption.
    /// </summary>
    /// <param name="token">The refresh token to validate.</param>
    /// <returns>True if the token contains no whitespace, false otherwise.</returns>
    /// <remarks>
    /// Base64 tokens should not contain spaces, tabs, or newlines. The presence of whitespace
    /// often indicates the token has been corrupted during transmission or storage.
    /// </remarks>
    public static bool BeNotContainWhitespace(string token)
    {
        return !string.IsNullOrEmpty(token) && !token.Any(char.IsWhiteSpace);
    }
    
    /// <summary>
    /// Logs out the user from all sessions by revoking all refresh tokens associated with their account.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Response indicating whether all sessions were terminated successfully.</returns>
    /// <example>POST /api/auth/logout-all</example>
    /// <remarks>
    /// <para><strong>Authentication Required:</strong> User must be authenticated to logout from all sessions.</para>
    /// <para>This endpoint provides a security feature to revoke ALL refresh tokens associated with the current user,
    /// effectively logging them out from all devices and sessions simultaneously.</para>
    /// 
    /// <para>This is useful in scenarios such as:</para>
    /// <list type="bullet">
    ///   <item><description>User suspects their account has been compromised</description></item>
    ///   <item><description>User wants to log out from all devices after a password change</description></item>
    ///   <item><description>User wants to revoke access from lost/stolen devices</description></item>
    ///   <item><description>Security maintenance operations</description></item>
    /// </list>
    /// 
    /// <para>After calling this endpoint:</para>
    /// <list type="number">
    ///   <item><description>All refresh tokens for the user are immediately revoked</description></item>
    ///   <item><description>Existing access tokens continue to work until natural expiration</description></item>
    ///   <item><description>User will need to re-authenticate on all devices/sessions</description></item>
    /// </list>
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/logout-all
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "success": true,
    ///         "message": "Successfully logged out from all sessions."
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">User was logged out from all sessions successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User was not found or no active sessions to revoke.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("logout-all", Name = "LogoutAll")]
    [Authorize]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var success = await tokenService.RevokeAllUserTokensAsync(currentUserId, cancellationToken);
        
        var response = new LogoutResponse
        (
            Success: success,
            Message: success ? "Successfully logged out from all sessions." : "Failed to logout from all sessions. No active sessions found."
        );

        return Ok(response);
    }
}
using Microsoft.AspNetCore.Http;
using Shortly.Core.Common.Abstractions;

namespace Shortly.Core.Common;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    private Guid? _userId;
    public Guid CurrentUserId => _userId ??= Guid.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
        ? id
        : throw new UnauthorizedAccessException("User ID claim is missing or invalid.");

    private string? _username;
    public string Username => _username ??= User?.FindFirst(ClaimTypes.Name)?.Value
                                            ?? throw new UnauthorizedAccessException("Username claim is missing.");

    private string? _email;
    public string CurrentUserEmail => _email ??= User?.FindFirst(ClaimTypes.Email)?.Value
                                      ?? throw new UnauthorizedAccessException("Email claim is missing.");

    private long? _permissions;
    public long Permissions => _permissions ??= long.TryParse(User?.FindFirst("Permissions")?.Value, out var p) 
        ? p 
        : 0; 

    private string? _jti;
    public string Jti => _jti ??= User?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;
}
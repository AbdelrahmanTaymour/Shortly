using System.Net;
using Microsoft.AspNetCore.Http;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.Enums;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Authentication;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private System.Security.Claims.ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        User?.GetUserId() ??
        throw new ApplicationException("User context is unavailable");

    public string Email => 
        User?.GetEmail() ??
        throw new ApplicationException("User context is unavailable");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public UserRole? Role => User?.GetUserRole();
    
    public Guid? OrganizationId => User?.GetOrganizationId();

    public Guid? MemberId => User?.GetMemberId();

    public MemberRole? MemberRole => User?.GetMemberRole();

    public enShortUrlOwnerType OwnerType =>
        !IsAuthenticated ? enShortUrlOwnerType.Anonymous :
        OrganizationId is not null ? enShortUrlOwnerType.Organization :
        enShortUrlOwnerType.User;

    public string? AnonymousSessionId => IsAuthenticated ? null : ResolveAnonymousSessionId();

    public string IpAddress => ResolveIpAddress();

    public bool IsAdmin => Role is UserRole.SuperAdmin or UserRole.SystemAdmin;

    public bool IsOwner(Guid ownerId) => IsAuthenticated && UserId == ownerId;

    private string ResolveAnonymousSessionId()
    {
        const string cookieName = "AnonId";
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return string.Empty;

        if (httpContext.Request.Cookies.TryGetValue(cookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
            return existing;

        var id = Guid.NewGuid().ToString();
        httpContext.Response.Cookies.Append(cookieName, id, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });
        return id;
    }

    private string ResolveIpAddress()
    {
        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
        if (ip is null || IPAddress.IsLoopback(ip)) return "127.0.0.1";
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) ip = ip.MapToIPv4();
        return ip.ToString();
    }
}
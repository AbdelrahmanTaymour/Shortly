using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.Infrastructure;

/// <summary>Mints access tokens with the same claims the real JwtService issues.</summary>
public static class TestTokens
{
    public static string Create(
        Guid userId,
        UserRole role = UserRole.User,
        Guid? organizationId = null,
        Guid? memberId = null,
        MemberRole? memberRole = null,
        bool expired = false,
        string signingKey = ShortlyApiFactory.JwtKey)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, $"user-{userId:N}"),
            new(ClaimTypes.Email, $"{userId:N}@example.com"),
            new(ClaimTypes.Role, role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (organizationId is not null)
        {
            claims.Add(new Claim("organization_id", organizationId.Value.ToString()));
            claims.Add(new Claim("member_id", (memberId ?? Guid.NewGuid()).ToString()));
            claims.Add(new Claim("member_role", (memberRole ?? MemberRole.Member).ToString()));
        }

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            ShortlyApiFactory.JwtIssuer,
            ShortlyApiFactory.JwtAudience,
            claims,
            notBefore: expired ? now.AddHours(-2) : now.AddMinutes(-1),
            expires: expired ? now.AddHours(-1) : now.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Authentication;

internal static class ClaimsPrincipalExtensions
{
    private const string OrganizationIdClaim = "organization_id";
    private const string MemberIdClaim = "member_id";
    private const string MemberRoleClaim = "member_role";

    public static Guid? GetUserId(this ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static string? GetUsername(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(ClaimTypes.Name);

    public static string? GetEmail(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(ClaimTypes.Email);

    public static string? GetJti(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

    public static UserRole? GetUserRole(this ClaimsPrincipal? principal) =>
        Enum.TryParse<UserRole>(principal?.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;

    public static MemberRole? GetMemberRole(this ClaimsPrincipal? principal) =>
        Enum.TryParse<MemberRole>(principal?.FindFirstValue(MemberRoleClaim), out var role) ? role : null;

    public static Guid? GetOrganizationId(this ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.FindFirstValue(OrganizationIdClaim), out var id) ? id : null;

    public static Guid? GetMemberId(this ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.FindFirstValue(MemberIdClaim), out var id) ? id : null;
}
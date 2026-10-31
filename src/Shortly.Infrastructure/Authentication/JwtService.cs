using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.RefreshToken;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Authentication;

internal sealed class JwtService(
    IConfiguration configuration,
    IRefreshTokenRepository refreshTokenRepository,
    IOrganizationMemberRepository memberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IJwtService
{
    private readonly IConfigurationSection _jwt = configuration.GetSection("Jwt");

    public Task<Result<AccessTokenResponse>> IssueTokensAsync(User user, CancellationToken cancellationToken = default) =>
        IssueTokensAsync(user, organizationId: null, memberRole: null, cancellationToken);

    public Task<Result<AccessTokenResponse>> IssueTokensAsync(User user, OrganizationMember membership, CancellationToken cancellationToken = default) =>
        IssueTokensAsync(user, membership.OrganizationId, membership.Role, cancellationToken);

    private async Task<Result<AccessTokenResponse>> IssueTokensAsync(User user, Guid? organizationId, MemberRole? memberRole, CancellationToken cancellationToken)
    {
        var accessToken = GenerateAccessToken(user, organizationId, memberRole);
        var expiresInSeconds = int.Parse(_jwt["AccessTokenExpirationMinutes"] ?? "15") * 60;
        var refreshLifetime = TimeSpan.FromDays(double.Parse(_jwt["RefreshTokenExpirationDays"] ?? "15"));

        var refreshTokenString = GenerateRefreshTokenString();
        var utcNow = dateTimeProvider.UtcNow;
        var refreshToken = RefreshToken.Issue(user.Id, Sha256(refreshTokenString), refreshLifetime, organizationId, utcNow);

        refreshTokenRepository.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken); // only commit here, now that the repository no longer self-commits

        return new AccessTokenResponse(accessToken, refreshTokenString, expiresInSeconds);
    }

    public async Task<Result<AccessTokenResponse>> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var stored = await refreshTokenRepository.GetByTokenHashAsync(Sha256(refreshToken), cancellationToken);
        if (stored is null || !stored.IsActive(utcNow))
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.InvalidRefreshToken);

        MemberRole? memberRole = null;
        var organizationId = stored.OrganizationId;

        if (organizationId is { } orgId)
        {
            var membership = await memberRepository.GetByOrganizationAndUserAsync(orgId, stored.UserId, cancellationToken);
            if (membership is null || !membership.IsActive) organizationId = null;
            else memberRole = membership.Role;
        }

        var newRefreshTokenString = GenerateRefreshTokenString();
        var refreshLifetime = TimeSpan.FromDays(double.Parse(_jwt["RefreshTokenExpirationDays"] ?? "15"));
        await refreshTokenRepository.RotateAsync(stored.Id, Sha256(newRefreshTokenString), refreshLifetime, utcNow, cancellationToken);

        var accessToken = GenerateAccessToken(stored.User, organizationId, memberRole);
        var expiresInSeconds = int.Parse(_jwt["AccessTokenExpirationMinutes"] ?? "15") * 60;

        return new AccessTokenResponse(accessToken, newRefreshTokenString, expiresInSeconds);
    }


    public async Task<Result> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var revoked = await refreshTokenRepository.RevokeAsync(Sha256(refreshToken), dateTimeProvider.UtcNow, cancellationToken);
        return revoked ? Result.Success() : Result.Failure(AuthenticationErrors.InvalidRefreshToken);
    }

    public async Task<Result> RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await refreshTokenRepository.RevokeAllAsync(userId, dateTimeProvider.UtcNow, cancellationToken);
        return Result.Success();
    }
    
    public string GenerateRedirectToken(string shortCode, TimeSpan lifetime)
    {
        var token = new JwtSecurityToken(
            _jwt["Issuer"], _jwt["Audience"],
            claims: [new Claim("shortCode", shortCode), new Claim("type", "redirect")],
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: SigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string? ValidateRedirectToken(string token)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(validateLifetime: true), out _);
            return principal.FindFirst("shortCode")?.Value;
        }
        catch { return null; }
    }

    private string GenerateAccessToken(User user, Guid? organizationId, MemberRole? memberRole)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (organizationId is { } orgId)
        {
            claims.Add(new Claim("organization_id", orgId.ToString()));
            if (memberRole is { } role) claims.Add(new Claim("member_role", role.ToString()));
        }

        var token = new JwtSecurityToken(
            _jwt["Issuer"], _jwt["Audience"], claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_jwt["AccessTokenExpirationMinutes"] ?? "15")),
            signingCredentials: SigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private SigningCredentials SigningCredentials() =>
        new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _jwt["Key"] ?? throw new InvalidOperationException("JWT signing key is missing."))),
            SecurityAlgorithms.HmacSha256);

    private TokenValidationParameters ValidationParameters(bool validateLifetime) => new()
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = validateLifetime, ValidateIssuerSigningKey = true,
        ValidIssuer = _jwt["Issuer"], ValidAudience = _jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt["Key"]!)),
        ClockSkew = TimeSpan.Zero
    };

    private static string GenerateRefreshTokenString() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string Sha256(string input) =>
        BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLower();
}
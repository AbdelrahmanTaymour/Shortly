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

    public Task<Result<AccessTokenResponse>> IssueTokensAsync(
        User user,
        CancellationToken cancellationToken = default) =>
        IssueTokensCoreAsync(user, membership: null, cancellationToken);

    public Task<Result<AccessTokenResponse>> IssueTokensAsync(
        User user,
        OrganizationMember membership,
        CancellationToken cancellationToken = default) =>
        IssueTokensCoreAsync(user, membership, cancellationToken);

    private async Task<Result<AccessTokenResponse>> IssueTokensCoreAsync(
        User user,
        OrganizationMember? membership,
        CancellationToken cancellationToken)
    {
        var accessToken = GenerateAccessToken(user, membership);

        var expiresInSeconds = int.Parse(_jwt["AccessTokenExpirationMinutes"] ?? "15") * 60;

        var refreshLifetime = TimeSpan.FromDays(double.Parse(_jwt["RefreshTokenExpirationDays"] ?? "15"));

        var refreshTokenString = GenerateRefreshTokenString();
        var utcNow = dateTimeProvider.UtcNow;

        var refreshToken = RefreshToken.Issue(
            user.Id,
            Sha256(refreshTokenString),
            refreshLifetime,
            membership?.OrganizationId,
            utcNow);

        refreshTokenRepository.Add(refreshToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AccessTokenResponse(accessToken, refreshTokenString, expiresInSeconds);
    }

    public async Task<Result<AccessTokenResponse>> RefreshAccessTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        var utcNow = dateTimeProvider.UtcNow;

        var stored = await refreshTokenRepository.GetByTokenHashAsync(Sha256(refreshToken), cancellationToken);

        if (stored is null || !stored.IsActive(utcNow))
        {
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.InvalidRefreshToken);
        }

        OrganizationMember? membership = null;

        if (stored.OrganizationId is { } organizationId)
        {
            membership = await memberRepository.GetByOrganizationAndUserAsync(
                organizationId,
                stored.UserId,
                cancellationToken);

            // to not issue an organization-scoped token for an inactive membership
            if (membership is not { IsActive: true })
                membership = null;
        }

        var newRefreshTokenString = GenerateRefreshTokenString();

        var refreshLifetime = TimeSpan.FromDays(double.Parse(_jwt["RefreshTokenExpirationDays"] ?? "15"));

        await refreshTokenRepository.RotateAsync(
            stored.Id,
            Sha256(newRefreshTokenString),
            refreshLifetime,
            utcNow,
            cancellationToken);

        var accessToken = GenerateAccessToken(stored.User, membership);

        var expiresInSeconds = int.Parse(_jwt["AccessTokenExpirationMinutes"] ?? "15") * 60;

        return new AccessTokenResponse(
            accessToken,
            newRefreshTokenString,
            expiresInSeconds);
    }

    public async Task<Result> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var revoked = await refreshTokenRepository
            .RevokeAsync(Sha256(refreshToken), dateTimeProvider.UtcNow, cancellationToken);

        return revoked
            ? Result.Success()
            : Result.Failure(AuthenticationErrors.InvalidRefreshToken);
    }

    public async Task<Result> RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await refreshTokenRepository.RevokeAllAsync(userId, dateTimeProvider.UtcNow, cancellationToken);
        return Result.Success();
    }

    public string GenerateRedirectToken(string shortCode, TimeSpan lifetime)
    {
        var token = new JwtSecurityToken(
            _jwt["Issuer"],
            _jwt["Audience"],
            claims:
            [
                new Claim("shortCode", shortCode),
                new Claim("type", "redirect")
            ],
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: SigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string? ValidateRedirectToken(string token)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, ValidationParameters(validateLifetime: true), out _);

            return principal.FindFirst("shortCode")?.Value;
        }
        catch
        {
            return null;
        }
    }

    private string GenerateAccessToken(User user, OrganizationMember? membership)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (membership is not null)
        {
            claims.Add(new Claim("organization_id", membership.OrganizationId.ToString()));
            claims.Add(new Claim("member_id", membership.Id.ToString()));
            claims.Add(new Claim("member_role", membership.Role.ToString()));
        }

        var token = new JwtSecurityToken(
            _jwt["Issuer"],
            _jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_jwt["AccessTokenExpirationMinutes"] ?? "15")),
            signingCredentials: SigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private SigningCredentials SigningCredentials() =>
        new(new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwt["Key"] ?? throw new InvalidOperationException("JWT signing key is missing."))),
            SecurityAlgorithms.HmacSha256);

    private TokenValidationParameters ValidationParameters(bool validateLifetime) =>
        new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwt["Issuer"],
            ValidAudience = _jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt["Key"]!)),
            ClockSkew = TimeSpan.Zero
        };

    private static string GenerateRefreshTokenString() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string Sha256(string input) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
}
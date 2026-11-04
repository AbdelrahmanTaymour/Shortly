using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Core.Abstractions.Authentication;

public interface IJwtService
{
    /// <summary>Mints an access and refresh token pair for a user (no organization context).</summary>
    Task<Result<AccessTokenResponse>> IssueTokensAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Mints an access and refresh token pair scoped to a specific organization membership.</summary>
    Task<Result<AccessTokenResponse>> IssueTokensAsync(User user, OrganizationMember membership, CancellationToken cancellationToken = default);

    /// <summary>Refreshes the token pair using a valid refresh token, preserving the original organization scope.</summary>
    Task<Result<AccessTokenResponse>> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes a single refresh token.</summary>
    Task<Result> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes all active refresh tokens for a given user.</summary>
    Task<Result> RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Gets the configured lifetime duration for refresh tokens.</summary>
    TimeSpan RefreshTokenLifetime { get; }

    /// <summary>Generates a short-lived signed JWT token for secure redirection or password verification flows.</summary>
    string GenerateRedirectToken(string shortCode, TimeSpan lifetime);

    /// <summary>Validates a redirect token and extracts the short code if valid.</summary>
    string? ValidateRedirectToken(string token);
}
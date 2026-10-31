using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Core.Abstractions.Authentication;

public interface IJwtService
{
    /// <summary>Mints an access + refresh token pair for a freshly authenticated user (no org context).</summary>
    Task<Result<AccessTokenResponse>> IssueTokensAsync(
        User user, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Mints an access + refresh token pair scoped to the given organization membership.
    ///     Used by Login (when the caller already knows which org) and by SwitchOrganization.
    /// </summary>
    Task<Result<AccessTokenResponse>> IssueTokensAsync(
        User user, OrganizationMember membership, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Refreshes the token pair. Preserves whatever OrganizationId was stored on the refresh token
    ///     row — refreshing is not a switch.
    /// </summary>
    Task<Result<AccessTokenResponse>> RefreshAccessTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default);

    Task<Result> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<Result> RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    string GenerateRedirectToken(string shortCode, TimeSpan lifetime);

    string? ValidateRedirectToken(string token);
}
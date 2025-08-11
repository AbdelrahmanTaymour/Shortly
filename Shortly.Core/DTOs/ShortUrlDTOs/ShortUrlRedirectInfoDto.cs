using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record ShortUrlRedirectInfoDto
{
    /// <summary>
    ///     Primary key of the ShortUrl in the database.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    ///     Short code used in the public-facing short link (eg. "a1b2C").
    /// </summary>
    public string? ShortCode { get; init; } = string.Empty;

    /// <summary>
    ///     The full original target URL to which the redirect should go.
    /// </summary>
    public string OriginalUrl { get; init; } = string.Empty;

    /// <summary>
    ///     Whether this short link is active (can be redirected to).
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    ///     Whether the link is password protected. If true, the repo/service can
    ///     ask the user for a password and call a verification endpoint.
    /// </summary>
    public bool IsPasswordProtected { get; init; }

    /// <summary>
    ///     (Optional) Password hash used by the service to verify an entered password.
    ///     This field should only be present for internal use by services/components
    ///     that are allowed to access hashed credentials. Avoid returning to clients.
    /// </summary>
    public string? PasswordHash { get; init; }

    /// <summary>
    ///     Maximum allowed clicks for this short URL. 0 means unlimited.
    /// </summary>
    public int ClickLimit { get; init; }

    /// <summary>
    ///     Current total clicks recorded. The redirect path may increment this
    ///     via a lightweight counter write or a dedicated analytics pipeline.
    /// </summary>
    public int TotalClicks { get; init; }

    /// <summary>
    ///     The expiry moment in UTC, if any. A null value means it does not expire.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    ///     Owner type (User, Organization, Anonymous). Useful for auditing/rate-limiting.
    /// </summary>
    public enShortUrlOwnerType OwnerType { get; init; }

    /// <summary>
    ///     Optional reference to the owning user.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    ///     Optional reference to the owning organization.
    /// </summary>
    public Guid? OrganizationId { get; init; }
    
    /// <summary>
    /// Determines if the URL can be accessed based on expiration and click limits.
    /// </summary>
    /// <returns>True if the URL can be accessed, otherwise false.</returns>
    public bool CanAccess()
    {
        if (!IsActive)
            return false;

        if (ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow)
            return false;

        if (ClickLimit > 0 && TotalClicks >= ClickLimit)
            return false;

        return true;
    }

}
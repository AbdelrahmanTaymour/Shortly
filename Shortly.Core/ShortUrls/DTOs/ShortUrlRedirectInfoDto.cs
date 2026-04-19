using Shortly.Domain.Enums;

namespace Shortly.Core.ShortUrls.DTOs;

public record ShortUrlRedirectInfoDto
{
    public long Id { get; init; }
    public string? ShortCode { get; init; } = string.Empty;
    public string OriginalUrl { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsPasswordProtected { get; init; }
    public string? PasswordHash { get; init; }
    public int ClickLimit { get; init; }
    public int TotalClicks { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public enShortUrlOwnerType OwnerType { get; init; }
    public Guid? UserId { get; init; }
    public Guid? OrganizationId { get; init; }
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
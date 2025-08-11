using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record ShortUrlDto
{
    public long Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public enShortUrlOwnerType OwnerType { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? CreatedByMemberId { get; set; }
    public bool IsActive { get; set; }
    public bool TrackingEnabled { get; set; }
    public int ClickLimit { get; set; }
    public int TotalClicks { get; set; }
    public bool IsPasswordProtected { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
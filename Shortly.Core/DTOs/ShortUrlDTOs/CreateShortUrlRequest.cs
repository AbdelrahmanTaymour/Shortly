using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record CreateShortUrlRequest
{
    public string OriginalUrl { get; set; }
    public string? CustomShortCode { get; set; }
    public int ClickLimit { get; set; } = -1; // -1 = unlimited
    public bool TrackingEnabled { get; set; } = true;
    public bool IsPasswordProtected { get; set; } = false;
    public string? Password { get; set; }
    public bool IsPrivate { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}
using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

/// <summary>
/// Define the ShortUrl class which acts as an entity model class to store Url details in the data store 
/// </summary>
public class ShortUrl
{
    public long Id { get; set; }
    public required string OriginalUrl { get; set; }
    public string? ShortCode { get; set; }

    // User Information
    public enShortUrlOwnerType OwnerType { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? CreatedByMemberId { get; set; } // Creator tracking (always populated for OrgMember)
    public string? AnonymousSessionId { get; set; } // Anonymous-specific fields (only used when OwnerType = Anonymous)
    public string? IpAddress { get; set; }

    // Configuration
    public bool IsActive { get; set; } = true;
    public bool TrackingEnabled { get; set; } = true;
    public int ClickLimit { get; set; }
    public int TotalClicks { get; set; } = 0;
    public bool IsPasswordProtected { get; set; } = false;
    public string? PasswordHash { get; set; }
    public bool IsPrivate { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }

    // Metadata
    public string? Title { get; set; }
    public string? Description { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public OrganizationMember? CreatedBy { get; set; }
    public Organization? Organization { get; set; }
    public ICollection<ClickEvent>? ClickEvents { get; set; }
}
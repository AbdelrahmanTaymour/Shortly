namespace Shortly.Domain.Entities;

public class ClickEvent
{
    public Guid Id { get; set; }
    public long ShortUrlId { get; set; }
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
 
    /// <summary>
    /// <para>
    /// ⚠️ Denormalized from ShortUrl.UserId
    /// populated on insert so analytics queries can seek directly
    /// on ClickEvents without joining through ShortUrls.
    /// </para>
    /// <para>Not a FK-constrained column; kept in sync via the application insert path in a background job</para>
    /// </summary>
    public Guid? UserId { get; set; }
 
    // Click tracking
    public required string IpAddress { get; set; }
    public required string SessionId { get; set; }
    public required string UserAgent { get; set; }
    public string? Referrer { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmTerm { get; set; }
    public string? UtmContent { get; set; }
 
    // GeoLocation
    public required string Country { get; set; }
    public required string City { get; set; }
 
    // UserAgent
    public required string Browser { get; set; }
    public required string OperatingSystem { get; set; }
    public required string Device { get; set; }
    public string? DeviceType { get; set; } // Desktop, Mobile, Tablet, Other
 
    // Traffic Source
    public string? ReferrerDomain { get; set; }
    public string? TrafficSource { get; set; } // Direct, Referral, Social, Search, Email, Campaign
 
    // Navigation properties
    public ShortUrl? ShortUrl { get; set; }
    // Note: No User navigation property — UserId is denormalized, not FK-constrained.
}

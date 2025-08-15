namespace Shortly.Core.Models;

public record ClickTrackingData(
    string IpAddress,
    string SessionId,
    string UserAgent,
    string? Referrer = null,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null,
    string? UtmTerm = null,
    string? UtmContent = null
);
namespace Shortly.Core.Abstractions.ClickTracking;

public sealed record ClickEventCapture(
    long ShortUrlId, DateTime ClickedAtUtc, string IpAddress, string SessionId, string UserAgent,
    string? Referrer, string? UtmSource, string? UtmMedium, string? UtmCampaign, string? UtmTerm, string? UtmContent);
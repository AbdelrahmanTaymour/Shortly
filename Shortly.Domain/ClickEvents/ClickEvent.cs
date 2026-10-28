using Shortly.Domain.Abstractions;

namespace Shortly.Domain.ClickEvents;

public sealed class ClickEvent : Entity<Guid>
{
    private ClickEvent(
        Guid id, long shortUrlId, DateTime clickedAtUtc,
        string ipAddress, string sessionId, string userAgent,
        string? referrer, string? referrerDomain, string? trafficSource,
        string? utmSource, string? utmMedium, string? utmCampaign, string? utmTerm, string? utmContent,
        string country, string city, string browser, string operatingSystem, string device, string? deviceType)
        : base(id)
    {
        ShortUrlId = shortUrlId;
        ClickedAtUtc = clickedAtUtc;
        IpAddress = ipAddress;
        SessionId = sessionId;
        UserAgent = userAgent;
        Referrer = referrer;
        ReferrerDomain = referrerDomain;
        TrafficSource = trafficSource;
        UtmSource = utmSource;
        UtmMedium = utmMedium;
        UtmCampaign = utmCampaign;
        UtmTerm = utmTerm;
        UtmContent = utmContent;
        Country = country;
        City = city;
        Browser = browser;
        OperatingSystem = operatingSystem;
        Device = device;
        DeviceType = deviceType;
    }

    private ClickEvent()
    {
    }

    public long ShortUrlId { get; private set; }
    public DateTime ClickedAtUtc { get; private set; }
    public string IpAddress { get; private set; } = null!;
    public string SessionId { get; private set; } = null!;
    public string UserAgent { get; private set; } = null!;
    public string? Referrer { get; private set; }
    public string? ReferrerDomain { get; private set; }
    public string? TrafficSource { get; private set; }
    public string? UtmSource { get; private set; }
    public string? UtmMedium { get; private set; }
    public string? UtmCampaign { get; private set; }
    public string? UtmTerm { get; private set; }
    public string? UtmContent { get; private set; }
    public string Country { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string Browser { get; private set; } = null!;
    public string OperatingSystem { get; private set; } = null!;
    public string Device { get; private set; } = null!;
    public string? DeviceType { get; private set; }

    public static ClickEvent Record(
        long shortUrlId, DateTime clickedAtUtc, string ipAddress, string sessionId, string userAgent,
        string? referrer, string? referrerDomain, string? trafficSource,
        string? utmSource, string? utmMedium, string? utmCampaign, string? utmTerm, string? utmContent,
        string country, string city, string browser, string operatingSystem, string device, string? deviceType)
    {
        return new ClickEvent(Guid.NewGuid(), shortUrlId, clickedAtUtc, ipAddress, sessionId, userAgent,
            referrer, referrerDomain, trafficSource, utmSource, utmMedium, utmCampaign, utmTerm, utmContent,
            country, city, browser, operatingSystem, device, deviceType);
    }
}
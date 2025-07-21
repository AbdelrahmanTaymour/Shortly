namespace Shortly.Core.Entities;

public class UrlAnalytics
{
    public int Id { get; set; }
    public int ShortUrlId { get; set; }
    public DateTime ClickedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referrer { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    
    // Navigation property
    public ShortUrl ShortUrl { get; set; } = null!;
}

public class AnalyticsSummary
{
    public int TotalClicks { get; set; }
    public int UniqueClicks { get; set; }
    public Dictionary<string, int> ClicksByCountry { get; set; } = new();
    public Dictionary<string, int> ClicksByDevice { get; set; } = new();
    public Dictionary<string, int> ClicksByBrowser { get; set; } = new();
    public Dictionary<DateTime, int> ClicksByDate { get; set; } = new();
    public List<string> TopReferrers { get; set; } = new();
}
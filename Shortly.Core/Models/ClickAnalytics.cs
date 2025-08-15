namespace Shortly.Core.Models;

public record ClickAnalytics
{
    public long ShortUrlId { get; set; }
    public long TotalClicks { get; set; }
    public long UniqueClicks { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Dictionary<string, int> ClicksByCountry { get; set; } = new();
    public Dictionary<string, int> ClicksByDeviceType { get; set; } = new();
    public Dictionary<string, int> ClicksByTrafficSource { get; set; } = new();
    public Dictionary<string, int> ClicksByBrowser { get; set; } = new();
    public Dictionary<string, int> ClicksByOperatingSystem { get; set; } = new();
    public Dictionary<DateTime, int> DailyClicks { get; set; } = new();
    public Dictionary<int, int> HourlyClicks { get; set; } = new();
    public List<string> TopReferrers { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
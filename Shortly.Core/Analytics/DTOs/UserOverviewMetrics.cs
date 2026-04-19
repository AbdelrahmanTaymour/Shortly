namespace Shortly.Core.Analytics.DTOs;

/// <summary>
///     Aggregate user-level metrics computable without session-level grouping.
///     URL shape comes from ShortUrls (no ClickEvents join).
///     Click totals come from a single COUNT_BIG pass on ClickEvents.
/// </summary>
public class UserOverviewMetrics
{
    // ── URL portfolio (ShortUrls table only — zero ClickEvents scan) ──────────
    public int TotalUrls { get; set; }
    public int ActiveUrls { get; set; }
    public int ExpiredUrls { get; set; }
    public int TotalTrackedUrls { get; set; }
    public DateTime? FirstUrlCreated { get; set; }
    public DateTime? MostRecentUrlCreated { get; set; }

    // ── Click aggregates (date-range scoped) ───────────
    public long TotalClicks { get; set; }
    public long UniqueClicks { get; set; }
    public double AverageClicksPerUrl { get; set; }
    public double AverageClicksPerDay { get; set; }
    public DateTime? FirstClickDate { get; set; }
    public DateTime? LastClickDate { get; set; }

    // ── Time-window counters (always ≤ 30-day scan) ───────────
    public long ClicksToday { get; set; }
    public long ClicksThisWeek { get; set; }
    public long ClicksThisMonth { get; set; }
}
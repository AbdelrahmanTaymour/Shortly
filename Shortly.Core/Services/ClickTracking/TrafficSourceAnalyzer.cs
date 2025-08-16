using Shortly.Core.Models;
using Shortly.Core.ServiceContracts.ClickTracking;

namespace Shortly.Core.Services.ClickTracking;

/// <inheritdoc />
public class TrafficSourceAnalyzer : ITrafficSourceAnalyzer
{
    /// <summary>
    /// Predefined collection of major search engine domains for traffic source identification.
    /// </summary>
    /// <remarks>
    /// This HashSet uses case-insensitive comparison and includes major global search engines.
    /// The collection is used to identify organic search traffic when analyzing referrer domains.
    /// Domains are stored without protocol or path components for efficient matching.
    /// </remarks>
    private readonly HashSet<string> _searchEngines = new(StringComparer.OrdinalIgnoreCase)
    {
        "google.com", "bing.com", "yahoo.com", "duckduckgo.com", "baidu.com", "yandex.com"
    };

    
    /// <summary>
    /// Predefined collection of major social media platform domains for traffic source identification.
    /// </summary>
    /// <remarks>
    /// This HashSet uses case-insensitive comparison and includes popular social media platforms.
    /// The collection helps identify social media traffic when analyzing referrer domains.
    /// Includes both traditional social networks and modern platforms like TikTok and messaging apps.
    /// </remarks>
    private readonly HashSet<string> _socialMediaSites = new(StringComparer.OrdinalIgnoreCase)
    {
        "facebook.com", "twitter.com", "instagram.com", "linkedin.com", "pinterest.com",
        "reddit.com", "tiktok.com", "youtube.com", "snapchat.com", "whatsapp.com"
    };
    
    
    /// <inheritdoc />
    public TrafficSourceInfo AnalyzeTrafficSource(string? referrer, string? utmSource, string? utmMedium)
    {
        // UTM parameters take precedence
        if (!string.IsNullOrEmpty(utmSource))
        {
            return new TrafficSourceInfo
            (
                DetermineTrafficSourceFromUtm(utmSource, utmMedium),
                ExtractDomainFromReferrer(referrer)
            );
        }

        // Analyze referrer
        if (!string.IsNullOrEmpty(referrer))
        {
            var referrerDomain = ExtractDomainFromReferrer((referrer));
            var trafficSource = DetermineTrafficSourceFromReferrer(referrerDomain);

            return new TrafficSourceInfo(trafficSource, referrerDomain);
        }

        // No referrer or UTM
        return new TrafficSourceInfo("Direct", null);
    }
   
    /// <inheritdoc />
    public string ExtractDomainFromReferrer(string? referrer)
    {
        if (string.IsNullOrEmpty(referrer))
            return string.Empty;

        try
        {
            var uri = new Uri(referrer);
            return uri.Host.ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }
    
    /// <inheritdoc />
    public bool IsSearchEngine(string? referrerDomain)
    {
        return !string.IsNullOrEmpty(referrerDomain) && _searchEngines.Any(referrerDomain.Contains);
    }
    
    /// <inheritdoc />
    public bool IsSocialMedia(string? referrerDomain)
    {
        return !string.IsNullOrEmpty(referrerDomain) && _socialMediaSites.Any(referrerDomain.Contains);
    }

    
    #region Private Helper Methods

    /// <summary>
    /// Determines traffic source category based on UTM parameters with intelligent medium-based classification.
    /// </summary>
    /// <param name="utmSource">The utm_source parameter value</param>
    /// <param name="utmMedium">The utm_medium parameter value (optional)</param>
    /// <returns>
    /// A string representing the classified traffic source category
    /// </returns>
    /// <remarks>
    /// Classification logic prioritizes utm_medium when available, then falls back to utm_source analysis:
    /// 
    /// **Medium-based classification:**
    /// - "email" → "Email"
    /// - "social" → "Social"  
    /// - "cpc", "ppc", "paid" → "Paid Search"
    /// - "organic" → "Organic Search"
    /// - "referral" → "Referral"
    /// - "display" → "Display"
    /// 
    /// **Source-based fallback** (when medium is not recognized):
    /// - Checks if utm_source contains social media platform names → "Social"
    /// - Checks if utm_source contains search engine names → "Search"
    /// - Default → "Campaign" (for custom campaign tracking)
    /// 
    /// This method enables sophisticated campaign tracking and provides meaningful
    /// categorization for marketing analytics and attribution reporting.
    /// All comparisons are case-insensitive for robust parameter handling.
    /// </remarks>
    /// <example>
    /// <code>
    /// DetermineTrafficSourceFromUtm("google", "cpc")        // Returns "Paid Search"
    /// DetermineTrafficSourceFromUtm("newsletter", "email")  // Returns "Email"
    /// DetermineTrafficSourceFromUtm("facebook", "social")   // Returns "Social"
    /// DetermineTrafficSourceFromUtm("custom-campaign", "")  // Returns "Campaign"
    /// </code>
    /// </example>
    private string DetermineTrafficSourceFromUtm(string utmSource, string? utmMedium)
    {
        var source = utmSource.ToLowerInvariant();
        var medium = utmMedium?.ToLowerInvariant();

        return medium switch
        {
            "email" => "Email",
            "social" => "Social",
            "cpc" or "ppc" or "paid" => "Paid Search",
            "organic" => "Organic Search",
            "referral" => "Referral",
            "display" => "Display",
            _ when _socialMediaSites.Any(sm => source.Contains(sm.Split('.')[0])) => "Social",
            _ when _searchEngines.Any(se => source.Contains(se.Split('.')[0])) => "Search",
            _ => "Campaign"
        };
    }
    
    
    /// <summary>
    /// Determines traffic source category based on referrer domain analysis.
    /// </summary>
    /// <param name="referrerDomain">The domain extracted from the referrer URL</param>
    /// <returns>
    /// A string representing the classified traffic source: "Direct", "Search", "Social", or "Referral"
    /// </returns>
    /// <remarks>
    /// Classification logic for referrer-based traffic:
    /// 1. **Direct**: No referrer domain provided (null/empty) - user typed URL or bookmarked
    /// 2. **Search**: Domain matches known search engines - organic search traffic
    /// 3. **Social**: Domain matches known social media platforms - social media traffic  
    /// 4. **Referral**: All other domains - traffic from other websites
    /// 
    /// This method provides the fallback classification when UTM parameters are not available.
    /// It's essential for tracking organic traffic and understanding natural user behavior.
    /// 
    /// The classification helps distinguish between:
    /// - Paid vs organic search traffic (when combined with UTM analysis)
    /// - Social media engagement vs other referral sources
    /// - Direct navigation vs referred traffic
    /// </remarks>
    /// <example>
    /// <code>
    /// DetermineTrafficSourceFromReferrer("google.com")      // Returns "Search"
    /// DetermineTrafficSourceFromReferrer("facebook.com")    // Returns "Social"  
    /// DetermineTrafficSourceFromReferrer("example.com")     // Returns "Referral"
    /// DetermineTrafficSourceFromReferrer(null)              // Returns "Direct"
    /// </code>
    /// </example>
    private string DetermineTrafficSourceFromReferrer(string? referrerDomain)
    {
        if (string.IsNullOrEmpty(referrerDomain))
            return "Direct";

        if (IsSearchEngine(referrerDomain))
            return "Search";

        if (IsSocialMedia(referrerDomain))
            return "Social";

        return "Referral";
    }

    #endregion
}
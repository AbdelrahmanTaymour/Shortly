using Shortly.Core.Models;

namespace Shortly.Core.ServiceContracts.ClickTracking;

/// <summary>
/// Analyzes traffic sources from referrer data and UTM parameters to categorize website traffic origins.
/// Provides intelligent classification of traffic into categories like Search, Social, Email, Direct, etc.
/// </summary>
/// <remarks>
/// This analyzer processes traffic attribution data to help understand how users arrive at your website.
/// It combines multiple data sources including:
/// - UTM campaign parameters (utm_source, utm_medium)
/// - HTTP referrer headers
/// - Predefined lists of search engines and social media platforms
/// 
/// Traffic source priority:
/// 1. UTM parameters take highest precedence (most reliable)
/// 2. Referrer analysis for organic traffic identification
/// 3. Direct traffic when no referrer information is available
/// 
/// The service maintains curated lists of major search engines and social media platforms
/// for accurate categorization of organic and social traffic.
/// </remarks>
public interface ITrafficSourceAnalyzer
{
    /// <summary>
    /// Analyzes traffic source information from UTM parameters and referrer data to determine origin classification.
    /// </summary>
    /// <param name="referrer">The HTTP referrer URL indicating the previous page that linked to the current page</param>
    /// <param name="utmSource">The utm_source parameter identifying the traffic source (e.g., "google", "newsletter")</param>
    /// <param name="utmMedium">The utm_medium parameter identifying the marketing medium (e.g., "cpc", "email", "social")</param>
    /// <returns>
    /// A <see cref="TrafficSourceInfo"/> object containing the classified traffic source and referrer domain.
    /// The traffic source will be categorized as Direct, Email, Social, Search, Paid Search, Display, Campaign, or Referral.
    /// </returns>
    /// <remarks>
    /// Analysis priority and logic:
    /// 1. **UTM Parameters** (highest priority): If utm_source is provided, uses UTM-based classification
    /// 2. **Referrer Analysis**: If no UTM but referrer exists, analyzes referrer domain
    /// 3. **Direct Traffic**: If neither UTM nor referrer, classifies as Direct
    /// 
    /// UTM-based classification uses utm_medium for primary categorization:
    /// - email → Email
    /// - social → Social  
    /// - cpc/ppc/paid → Paid Search
    /// - organic → Organic Search
    /// - referral → Referral
    /// - display → Display
    /// 
    /// Referrer-based classification:
    /// - Search engines → Search
    /// - Social media platforms → Social
    /// - Other domains → Referral
    /// 
    /// This method provides the foundation for traffic analytics and attribution reporting.
    /// </remarks>
    /// <example>
    /// <code>
    /// var analyzer = new TrafficSourceAnalyzer();
    /// 
    /// // UTM-based analysis
    /// var utmResult = analyzer.AnalyzeTrafficSource(null, "google", "cpc");
    /// // Returns: TrafficSource = "Paid Search", ReferrerDomain = ""
    /// 
    /// // Referrer-based analysis  
    /// var referrerResult = analyzer.AnalyzeTrafficSource("https://google.com/search?q=example", null, null);
    /// // Returns: TrafficSource = "Search", ReferrerDomain = "google.com"
    /// 
    /// // Direct traffic
    /// var directResult = analyzer.AnalyzeTrafficSource(null, null, null);
    /// // Returns: TrafficSource = "Direct", ReferrerDomain = null
    /// </code>
    /// </example>
    TrafficSourceInfo AnalyzeTrafficSource(string? referrer, string? utmSource, string? utmMedium);
    
    /// <summary>
    /// Extracts the domain name from a referrer URL.
    /// </summary>
    /// <param name="referrer">The full referrer URL to parse</param>
    /// <returns>
    /// The lowercase domain name (host) from the URL, or empty string if extraction fails
    /// </returns>
    /// <remarks>
    /// This method safely parses URLs to extract just the domain component:
    /// - Removes protocol (http/https)
    /// - Removes path, query parameters, and fragments  
    /// - Converts to lowercase for consistent comparison
    /// - Handles malformed URLs gracefully by returning empty string
    /// 
    /// The extracted domain is used for matching against search engine and social media lists.
    /// Error handling ensures that malformed referrer URLs don't cause exceptions.
    /// </remarks>
    /// <example>
    /// <code>
    /// ExtractDomainFromReferrer("https://www.google.com/search?q=test")  // Returns "www.google.com"
    /// ExtractDomainFromReferrer("https://facebook.com/page/post/123")     // Returns "facebook.com"
    /// ExtractDomainFromReferrer("invalid-url")                           // Returns ""
    /// </code>
    /// </example>
    string ExtractDomainFromReferrer(string? referrer);
   
    
    /// <summary>
    /// Determines if the provided domain belongs to a known search engine.
    /// </summary>
    /// <param name="referrerDomain">The domain name to check against search engine list</param>
    /// <returns>
    /// True if the domain is identified as a search engine; otherwise false
    /// </returns>
    /// <remarks>
    /// This method performs a case-insensitive partial match against the predefined search engine list.
    /// It uses 'Contains' logic, so "www.google.com" matches "google.com" in the search engine list.
    /// 
    /// The method is designed to handle various subdomains of major search engines:
    /// - google.com matches www.google.com, images.google.com, etc.
    /// - The matching is flexible to accommodate different regional or specialized search engine domains
    /// 
    /// Returns false for null or empty domain strings to ensure safe operation.
    /// </remarks>
    /// <example>
    /// <code>
    /// IsSearchEngine("google.com")        // Returns true
    /// IsSearchEngine("www.google.com")    // Returns true  
    /// IsSearchEngine("facebook.com")      // Returns false
    /// IsSearchEngine(null)                // Returns false
    /// </code>
    /// </example>
    bool IsSearchEngine(string? referrerDomain);
   
    
    /// <summary>
    /// Determines if the provided domain belongs to a known social media platform.
    /// </summary>
    /// <param name="referrerDomain">The domain name to check against social media platform list</param>
    /// <returns>
    /// True if the domain is identified as a social media platform; otherwise false
    /// </returns>
    /// <remarks>
    /// This method performs a case-insensitive partial match against the predefined social media list.
    /// It uses 'Contains' logic to handle various subdomains and variations of social media platforms.
    /// 
    /// The method accommodates different social media domain patterns:
    /// - facebook.com matches www.facebook.com, m.facebook.com, etc.
    /// - Includes both traditional social networks and modern platforms
    /// - Covers messaging platforms that drive significant web traffic
    /// 
    /// Returns false for null or empty domain strings to ensure safe operation.
    /// This is essential for accurate social media traffic attribution.
    /// </remarks>
    /// <example>
    /// <code>
    /// IsSocialMedia("facebook.com")       // Returns true
    /// IsSocialMedia("m.twitter.com")      // Returns true
    /// IsSocialMedia("google.com")         // Returns false
    /// IsSocialMedia("")                   // Returns false
    /// </code>
    /// </example>
    bool IsSocialMedia(string? referrerDomain);
}
using System.Text.RegularExpressions;
using Shortly.Core.Models;
using Shortly.Core.ServiceContracts.ClickTracking;

namespace Shortly.Core.Services.ClickTracking;

/// <inheritdoc />
public class UserAgentParsingService : IUserAgentParsingService
{
    /// <inheritdoc />
    public UserAgentInfo ParseUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return new UserAgentInfo("Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown");

        var browser = ExtractBrowser(userAgent);
        var os = ExtractOperatingSystem(userAgent);
        var device = ExtractDevice(userAgent);
        var deviceType = DetermineDeviceType(userAgent);

        return new UserAgentInfo(browser.Name, os.Name, device, deviceType, browser.Version, os.Version);
    }


    #region Private Helper Methods

    /// <summary>
    /// Extracts the browser name and version from the User-Agent string.
    /// </summary>
    /// <param name="userAgent">The User-Agent string to analyze</param>
    /// <returns>
    /// A tuple containing the browser name and version.
    /// Returns ("Unknown", "Unknown") if the browser cannot be identified.
    /// </returns>
    /// <remarks>
    /// Supports detection of major browsers in order of specificity:
    /// - Microsoft Edge (identified by "edg/" token)
    /// - Google Chrome (identified by "chrome/" but not Edge)
    /// - Mozilla Firefox (identified by "firefox/")
    /// - Safari (identified by "safari/" without Chrome)
    /// - Opera (identified by "opr/" or "opera/")
    /// 
    /// The method uses case-sensitive string matching for performance and follows
    /// the typical order browsers appear in User-Agent strings to avoid misidentification.
    /// Edge detection comes first since Edge User-Agents also contain "chrome/".
    /// </remarks>
    private (string Name, string Version) ExtractBrowser(string userAgent)
    {
        
        if(userAgent.Contains("edg/"))
            return ("Microsoft Edge", ExtractVersion(userAgent, "edg/"));
        
        if(userAgent.Contains("chrome/") && !userAgent.Contains("edg"))
            return ("Chrome", ExtractVersion(userAgent, "chrome/"));
        
        if(userAgent.Contains("firefox/"))
            return ("Firefox", ExtractVersion(userAgent, "firefox/"));
        
        if(userAgent.Contains("safari/") && !userAgent.Contains("chrome/"))
            return ("Safari", ExtractVersion(userAgent, "version/"));
        
        if (userAgent.Contains("opr/") || userAgent.Contains("opera/"))
            return ("Opera", ExtractVersion(userAgent, userAgent.Contains("opr/") ? "opr/" : "opera/"));
        
        return ("Unknown", "Unknown");
    }
    
    
    /// <summary>
    /// Extracts operating system name and version from the User-Agent string.
    /// </summary>
    /// <param name="userAgent">The User-Agent string to analyze</param>
    /// <returns>
    /// A tuple containing the operating system name and version.
    /// Returns ("Unknown", "Unknown") if the OS cannot be identified.
    /// </returns>
    /// <remarks>
    /// Supports detection of major operating systems:
    /// - Windows (with specific version mapping from NT versions)
    /// - macOS (extracted from "mac os x" token)
    /// - Android (with version extraction)
    /// - iOS (from both "iphone os" and "ios" tokens)
    /// - Linux (generic detection without version)
    /// 
    /// Windows version mapping:
    /// - NT 10.0 = Windows 10
    /// - NT 6.3 = Windows 8.1
    /// - NT 6.2 = Windows 8
    /// - NT 6.1 = Windows 7
    /// 
    /// The method converts the User-Agent to lowercase for case-insensitive matching.
    /// </remarks>
    private (string Name, string Version) ExtractOperatingSystem(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();
        
        if (ua.Contains("windows nt"))
        {
            if (ua.Contains("windows nt 10.0")) return ("Windows", "10");
            if (ua.Contains("windows nt 6.3")) return ("Windows", "8.1");
            if (ua.Contains("windows nt 6.2")) return ("Windows", "8");
            if (ua.Contains("windows nt 6.1")) return ("Windows", "7");
            return ("Windows", "Unknown");
        }

        if (ua.Contains("mac os x"))
            return ("macOS", ExtractVersion(userAgent, "mac os x "));

        if (ua.Contains("android"))
            return ("Android", ExtractVersion(userAgent, "android "));

        if (ua.Contains("iphone os") || ua.Contains("ios"))
            return ("iOS", ExtractVersion(userAgent, ua.Contains("iphone os") ? "iphone os " : "ios "));

        if (ua.Contains("linux"))
            return ("Linux", "Unknown");

        return ("Unknown", "Unknown");
    }
    
    
    /// <summary>
    /// Identifies the specific device model or type from the User-Agent string.
    /// </summary>
    /// <param name="userAgent">The User-Agent string to analyze</param>
    /// <returns>
    /// A string representing the device model or manufacturer.
    /// Returns "Desktop" if no mobile device is detected.
    /// </returns>
    /// <remarks>
    /// Device detection priority:
    /// 1. Apple devices (iPhone, iPad) - exact model identification
    /// 2. Android devices - differentiated between phones and tablets
    /// 3. Manufacturer-specific devices (Samsung, Huawei, Xiaomi, OnePlus)
    /// 4. Default to "Desktop" for unidentified devices
    /// 
    /// For Android devices:
    /// - Presence of "mobile" indicates Android Phone
    /// - Absence of "mobile" indicates Android Tablet
    /// 
    /// Manufacturer detection is generic and returns "{Brand} Device" format.
    /// The method uses case-insensitive matching for broader compatibility.
    /// </remarks>
    private string ExtractDevice(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();

        if (ua.Contains("iphone")) return "iPhone";
        if (ua.Contains("ipad")) return "iPad";
        if (ua.Contains("android"))
        {
            if (ua.Contains("mobile")) return "Android Phone";
            return "Android Tablet";
        }

        // Extract device model for common patterns
        if (ua.Contains("samsung")) return "Samsung Device";
        if (ua.Contains("huawei")) return "Huawei Device";
        if (ua.Contains("xiaomi")) return "Xiaomi Device";
        if (ua.Contains("oneplus")) return "OnePlus Device";

        return "Desktop";
    }
    
    
    /// <summary>
    /// Determines the broad category of device type (Mobile, Tablet, or Desktop).
    /// </summary>
    /// <param name="userAgent">The User-Agent string to analyze</param>
    /// <returns>
    /// A string indicating the device type: "Mobile", "Tablet", or "Desktop"
    /// </returns>
    /// <remarks>
    /// Classification logic:
    /// - "Mobile": Contains "mobile" or "iphone" tokens
    /// - "Tablet": Contains "tablet" or "ipad" tokens, or Android without "mobile"
    /// - "Desktop": Default for all other cases
    /// 
    /// The Android classification is nuanced:
    /// - Android + mobile = Mobile device
    /// - Android without mobile = Tablet (common pattern for Android tablets)
    /// 
    /// This classification is useful for responsive design decisions and analytics.
    /// The method uses case-insensitive matching and prioritizes mobile detection.
    /// </remarks>
    private string DetermineDeviceType(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();

        if (ua.Contains("mobile") || ua.Contains("iphone"))
            return "Mobile";

        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "Tablet";

        if (ua.Contains("android") && !ua.Contains("mobile"))
            return "Tablet";

        return "Desktop";
    }
    
    
    /// <summary>
    /// Extracts version information from the User-Agent string using pattern matching.
    /// </summary>
    /// <param name="userAgent">The User-Agent string containing version information</param>
    /// <param name="pattern">The pattern string that precedes the version number</param>
    /// <returns>
    /// The major version number as a string, or "Unknown" if extraction fails
    /// </returns>
    /// <remarks>
    /// This method uses regular expressions to find version patterns following a specific token.
    /// The regex pattern matches digits, underscores, and dots after the specified pattern.
    /// 
    /// Processing steps:
    /// 1. Escapes the input pattern for regex safety
    /// 2. Matches version patterns (digits, dots, underscores)
    /// 3. Replaces underscores with dots (common in iOS versions)
    /// 4. Returns only the major version (first number before any dot)
    /// 
    /// The method is case-insensitive and handles various version formats commonly
    /// found in User-Agent strings from different browsers and operating systems.
    /// </remarks>
    /// <example>
    /// ExtractVersion("Chrome/91.0.4472.124", "chrome/") returns "91"
    /// ExtractVersion("iPhone OS 14_6", "iPhone OS ") returns "14"
    /// </example>
    private string ExtractVersion(string userAgent, string pattern)
    {
        var match = Regex.Match(userAgent, $@"(?<={Regex.Escape(pattern)})[\d_\.]+", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.Replace("_", ".").Split('.')[0] : "Unknown";
    }

    #endregion
}
using Shortly.Core.Models;

namespace Shortly.Core.ServiceContracts.ClickTracking;

/// <summary>
/// Service for parsing User-Agent strings to extract browser, operating system, device, and version information.
/// Provides detailed analysis of client environment data from HTTP User-Agent headers.
/// </summary>
/// <remarks>
/// This service analyzes User-Agent strings to identify:
/// - Browser type and version (Chrome, Firefox, Safari, Edge, Opera)
/// - Operating system and version (Windows, macOS, Android, iOS, Linux)
/// - Device information (iPhone, iPad, Android devices, Desktop)
/// - Device type classification (Mobile, Tablet, Desktop)
/// 
/// The parsing uses pattern matching and string analysis techniques to extract
/// meaningful information from the often complex and varied User-Agent formats.
/// </remarks>
public interface IUserAgentParsingService
{
    /// <summary>
    /// Parses a User-Agent string and extracts comprehensive client information.
    /// </summary>
    /// <param name="userAgent">The User-Agent string from the HTTP request header</param>
    /// <returns>
    /// A <see cref="UserAgentInfo"/> object containing parsed browser, OS, device, and version information.
    /// If parsing fails or the User-Agent is invalid, returns default "Unknown" values.
    /// </returns>
    /// <remarks>
    /// This method orchestrates the parsing process by:
    /// 1. Validating the input User-Agent string
    /// 2. Extracting browser information and version
    /// 3. Determining operating system and version
    /// 4. Identifying specific device model
    /// 5. Classifying the device type (Mobile/Tablet/Desktop)
    /// 
    /// The parsing is designed to handle common User-Agent formats from major browsers
    /// and operating systems. For unrecognized patterns, "Unknown" values are returned.
    /// </remarks>
    /// <example>
    /// <code>
    /// var service = new UserAgentParsingService();
    /// var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
    /// var info = service.ParseUserAgent(userAgent);
    /// // Returns: Chrome browser, Windows 10, Desktop device
    /// </code>
    /// </example>
    UserAgentInfo ParseUserAgent(string userAgent);
}
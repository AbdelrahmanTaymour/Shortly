using System.Text.RegularExpressions;

namespace Shortly.Core.Services;

public interface ISecurityService
{
    Task<bool> IsUrlSafeAsync(string url);
    Task<bool> IsCustomShortCodeAllowedAsync(string shortCode);
    string SanitizeUrl(string url);
    bool IsValidUrl(string url);
}

public class SecurityService : ISecurityService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SecurityService> _logger;
    private readonly List<string> _blockedDomains;
    private readonly List<string> _reservedShortCodes;

    public SecurityService(HttpClient httpClient, ILogger<SecurityService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _blockedDomains = LoadBlockedDomains();
        _reservedShortCodes = LoadReservedShortCodes();
    }

    public async Task<bool> IsUrlSafeAsync(string url)
    {
        try
        {
            // Basic domain check
            var uri = new Uri(url);
            if (_blockedDomains.Any(domain => uri.Host.Contains(domain, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // TODO: Integrate with external URL scanning services like VirusTotal, Google Safe Browsing
            // For now, implement basic checks
            return await PerformBasicSecurityChecks(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking URL safety for {Url}", url);
            return false;
        }
    }

    public async Task<bool> IsCustomShortCodeAllowedAsync(string shortCode)
    {
        // Check against reserved words and system routes
        return !_reservedShortCodes.Contains(shortCode.ToLower()) &&
               !shortCode.Contains("admin", StringComparison.OrdinalIgnoreCase) &&
               !shortCode.Contains("api", StringComparison.OrdinalIgnoreCase) &&
               Regex.IsMatch(shortCode, @"^[a-zA-Z0-9_-]+$") &&
               shortCode.Length >= 3 && shortCode.Length <= 50;
    }

    public string SanitizeUrl(string url)
    {
        // Remove dangerous parameters and normalize URL
        var uri = new Uri(url);
        var cleanUrl = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
        
        // Add back safe query parameters if needed
        if (!string.IsNullOrEmpty(uri.Query))
        {
            // Filter out potentially dangerous parameters
            var safeQuery = FilterQueryParameters(uri.Query);
            if (!string.IsNullOrEmpty(safeQuery))
                cleanUrl += safeQuery;
        }

        return cleanUrl;
    }

    public bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private async Task<bool> PerformBasicSecurityChecks(string url)
    {
        // Check if URL is reachable and not a redirect loop
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private List<string> LoadBlockedDomains()
    {
        return new List<string>
        {
            "malware.com",
            "phishing.net",
            "spam.org"
            // Load from configuration or database
        };
    }

    private List<string> LoadReservedShortCodes()
    {
        return new List<string>
        {
            "api", "admin", "www", "mail", "ftp", "localhost",
            "dashboard", "analytics", "stats", "settings"
        };
    }

    private string FilterQueryParameters(string query)
    {
        // Implementation to filter out dangerous query parameters
        var dangerousParams = new[] { "javascript:", "data:", "vbscript:" };
        
        foreach (var param in dangerousParams)
        {
            if (query.Contains(param, StringComparison.OrdinalIgnoreCase))
                return string.Empty;
        }

        return query;
    }
}
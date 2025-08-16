using Microsoft.Extensions.Logging;
using Shortly.Core.Models;
using Shortly.Core.RepositoryContract.ClickTracking;

namespace Shortly.Infrastructure.Repositories.ClickTracking;

/// <summary>
/// Provides geolocation lookup functionality for a given IP address using the ipapi.co API.
/// </summary>
/// <remarks>
/// This service queries an external geolocation API to retrieve country, city, region, and coordinate information.
/// If the IP address is private, local, or invalid, it returns default "Unknown" values.
/// </remarks>
public class GeoLocationService(HttpClient httpClient, ILogger<GeoLocationService> logger) : IGeoLocationService
{
    
    /// <inheritdoc />>
    public async Task<GeoLocationInfo> GetLocationInfoAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            if(IsPrivateOrLocalIp(ipAddress))
                return new GeoLocationInfo("Unknown", "Unknown", "Unknown", "Unknown");

            var response = await httpClient.GetStringAsync($"https://ipapi.co/{ipAddress}/json/", cancellationToken);
            var geoData = System.Text.Json.JsonSerializer.Deserialize<GeoApiResponse>(response);
            return new GeoLocationInfo
            (
                geoData?.country_name ?? "Unknown",
                geoData?.city ?? "Unknown",
                geoData?.country_code ?? "Unknown",
                geoData?.region ?? "Unknown",
                geoData?.latitude,
                geoData?.longitude
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get geolocation for IP: {IpAddress}", ipAddress);
            return new GeoLocationInfo("Unknown", "Unknown", "Unknown", "Unknown");
        }
    }

   
    /// <summary>
    /// Determines whether the specified IP address is private, local, or invalid.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <returns>
    /// <c>true</c> if the IP address is private, local, or empty; otherwise, <c>false</c>.
    /// </returns>
    private bool IsPrivateOrLocalIp(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return true;
        
        return ipAddress == "127.0.0.1" || 
               ipAddress == "::1" || 
               ipAddress.StartsWith("192.168.") ||
               ipAddress.StartsWith("10.") ||
               ipAddress.StartsWith("172.16.") ||
               ipAddress == "localhost";
    }
    
    
    /// <summary>
    /// Represents the raw response from the geolocation API.
    /// </summary>
    private class GeoApiResponse
    {
        public string? country_name { get; set; }
        public string? country_code { get; set; }
        public string? city { get; set; }
        public string? region { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }
    }
}
using Shortly.Core.Models;

namespace Shortly.Core.RepositoryContract.ClickTracking;

/// <summary>
/// This service queries an external geolocation API to retrieve country, city, region, and coordinate information.
/// If the IP address is private, local, or invalid, it returns default "Unknown" values.
/// </summary>
public interface IGeoLocationService
{
    /// <summary>
    /// Retrieves geolocation information for a specified IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address to look up.</param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="GeoLocationInfo"/> object containing country, city, region, and coordinates.
    /// Returns "Unknown" values if the IP address is private/local or if an error occurs.
    /// </returns>
    Task<GeoLocationInfo> GetLocationInfoAsync(string ipAddress, CancellationToken cancellationToken = default);
}
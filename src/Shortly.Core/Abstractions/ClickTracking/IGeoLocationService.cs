namespace Shortly.Core.Abstractions.ClickTracking;

public interface IGeoLocationService
{
    Task<GeoLocationInfo> LookupAsync(string ipAddress, CancellationToken cancellationToken);
}
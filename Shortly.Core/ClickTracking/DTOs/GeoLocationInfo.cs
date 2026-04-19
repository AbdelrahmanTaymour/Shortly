namespace Shortly.Core.ClickTracking.DTOs;

public record GeoLocationInfo(
    string Country,
    string City,
    string CountryCode,
    string Region,
    double? Latitude = null,
    double? Longitude = null
);
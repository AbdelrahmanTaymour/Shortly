namespace Shortly.Core.ClickTracking.DTOs;

public record TrafficSourceInfo(
    string TrafficSource,
    string? ReferrerDomain
);
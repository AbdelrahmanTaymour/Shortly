namespace Shortly.Core.ClickTracking.DTOs;

public record UserAgentInfo(
    string Browser,
    string OperatingSystem,
    string Device,
    string DeviceType,
    string BrowserVersion,
    string OsVersion
);
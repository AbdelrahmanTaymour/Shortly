namespace Shortly.Core.Models;

public record UserAgentInfo(
    string Browser,
    string OperatingSystem,
    string Device,
    string DeviceType,
    string BrowserVersion,
    string OsVersion
);
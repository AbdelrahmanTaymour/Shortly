using Shortly.Core.Abstractions.ClickTracking;

namespace Shortly.Infrastructure.ClickTracking;

internal sealed class UserAgentParser : IUserAgentParser
{
    public UserAgentInfo Parse(string userAgent)
    {
        var client = global::UAParser.Parser.GetDefault().Parse(userAgent);
        var deviceType = ClassifyDeviceType(userAgent);

        return new UserAgentInfo(client.UA.Family, client.OS.Family, client.Device.Family, deviceType);
    }

    private static string ClassifyDeviceType(string userAgent)
    {
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase)) return "Tablet";
        if (userAgent.Contains("Mobi", StringComparison.OrdinalIgnoreCase)) return "Mobile";
        return "Desktop";
    }
}
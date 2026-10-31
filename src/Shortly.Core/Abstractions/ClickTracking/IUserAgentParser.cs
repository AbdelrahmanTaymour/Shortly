namespace Shortly.Core.Abstractions.ClickTracking;

public interface IUserAgentParser
{
    UserAgentInfo Parse(string userAgent);
}

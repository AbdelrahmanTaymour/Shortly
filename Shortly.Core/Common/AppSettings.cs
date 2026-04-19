using Shortly.Core.Tokens.DTOs;

namespace Shortly.Core.Common;

public record AppSettings
{
    public const string SectionName = "AppSettings";
    public string CompanyName { get; set; } = "Shortly";
    public string BaseUrl { get; set; }
    public string LogoUrl { get; set; }
    public string SupportEmail { get; set; }
    public TokenSettings Tokens { get; set; } = new TokenSettings();
}
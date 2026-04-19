namespace Shortly.Core.Email.DTOs;

public record EmailSettings
{
    public const string SectionName = "EmailSettings";
    public string Provider { get; set; } = "smtp";
    public SmtpSettings Smtp { get; set; } = new SmtpSettings();
    public GeneralSettings General { get; set; } = new GeneralSettings();
}
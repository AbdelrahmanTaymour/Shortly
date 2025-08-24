using Microsoft.Extensions.Options;
using Shortly.Core.Models;

namespace Shortly.Core.Validators.Configuration;

public class EmailSettingsValidator : IValidateOptions<EmailSettings>
{
    public ValidateOptionsResult Validate(string? name, EmailSettings options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            errors.Add("Email provider must be specified");
        }

        switch (options.Provider.ToLower())
        {
            case "smtp":
                ValidateSmtpSettings(options.Smtp, errors);
                break;
            default:
                errors.Add($"Unsupported email provider: {options.Provider}");
                break;
        }

        return errors.Any() 
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
    
    
    private static void ValidateSmtpSettings(SmtpSettings smtp, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(smtp.Host))
            errors.Add("SMTP Host is required");

        if (smtp.Port <= 0 || smtp.Port > 65535)
            errors.Add("SMTP Port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(smtp.Username))
            errors.Add("SMTP Username is required");

        if (string.IsNullOrWhiteSpace(smtp.Password))
            errors.Add("SMTP Password is required");

        if (string.IsNullOrWhiteSpace(smtp.FromEmail))
            errors.Add("SMTP FromEmail is required");

        if (!IsValidEmail(smtp.FromEmail))
            errors.Add("SMTP FromEmail must be a valid email address");
    }
    
    
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
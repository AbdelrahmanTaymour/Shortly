using Microsoft.Extensions.Options;
using Shortly.Core.Models;

namespace Shortly.Core.Validators.Configuration;

public class AppSettingsValidator : IValidateOptions<AppSettings>
{
    public ValidateOptionsResult Validate(string? name, AppSettings options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Name))
            errors.Add("App Name is required");

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            errors.Add("App BaseUrl is required");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            errors.Add("App BaseUrl must be a valid URL");

        if (!string.IsNullOrWhiteSpace(options.SupportEmail) && !IsValidEmail(options.SupportEmail))
            errors.Add("Support email must be a valid email address");

        if (options.Tokens.EmailVerificationExpiryHours <= 0)
            errors.Add("Email verification expiry hours must be greater than 0");

        if (options.Tokens.PasswordResetExpiryHours <= 0)
            errors.Add("Password reset expiry hours must be greater than 0");

        if (options.Tokens.InvitationExpiryDays <= 0)
            errors.Add("Invitation expiry days must be greater than 0");

        return errors.Any() 
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static bool IsValidEmail(string email)
    {
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
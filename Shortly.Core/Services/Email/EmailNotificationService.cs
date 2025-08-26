using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shortly.Core.Models;
using Shortly.Core.ServiceContracts.Email;

namespace Shortly.Core.Services.Email;

/// <inheritdoc />
/// <remarks>
///     This service leverages <see cref="IEmailTemplateService" /> to generate email content
///     and <see cref="IEmailService" /> to send messages. It builds URLs using configuration
///     values (e.g., <c>AppSettings:BaseUrl</c>) and logs all email operations.
/// </remarks>
public class EmailNotificationService(
    IEmailService emailService,
    IEmailTemplateService templateService,
    IConfiguration configuration,
    ILogger<EmailNotificationService> logger)
    : IEmailNotificationService
{
    /// <inheritdoc />
    public async Task<EmailResult> SendEmailVerificationAsync(string email, string userName, string verificationToken)
    {
        try
        {
            var baseUrl = configuration["AppSettings:BaseUrl"];
            var verificationLink =
                $"{baseUrl}/verify-email?token={verificationToken}&email={Uri.EscapeDataString(email)}";

            var template = templateService.GetEmailVerificationTemplateAsync(userName, verificationLink);

            var request = new EmailRequest
            {
                To = email,
                Subject = template.Subject,
                Body = template.Body,
                IsHtml = template.IsHtml
            };

            logger.LogInformation("Sending email verification to {Email}", email);
            return await emailService.SendAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email verification to {Email}", email);
            return EmailResult.Failure("Failed to send email verification", ex);
        }
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendPasswordResetAsync(string email, string userName, string resetToken)
    {
        try
        {
            var baseUrl = configuration["AppSettings:BaseUrl"];
            var resetLink = $"{baseUrl}/reset-password?token={resetToken}&email={Uri.EscapeDataString(email)}";

            var template = templateService.GetPasswordResetTemplateAsync(userName, resetLink);

            var request = new EmailRequest
            {
                To = email,
                Subject = template.Subject,
                Body = template.Body,
                IsHtml = template.IsHtml
            };

            logger.LogInformation("Sending password reset email to {Email}", email);
            return await emailService.SendAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            return EmailResult.Failure("Failed to send password reset email", ex);
        }
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendUserInvitationAsync(string email, string inviterUsername, string inviteeName,
        string? invitationToken,
        string organizationName)
    {
        try
        {
            var baseUiUrl = configuration["AppSettings:BaseUrl"];
            var invitationLink = $"{baseUiUrl}/api/organization-invitations/accept?token={Uri.EscapeDataString(invitationToken)}";

            var template =
                templateService.GetUserInvitationTemplateAsync(inviterUsername, inviteeName, invitationLink,
                    organizationName);

            var request = new EmailRequest
            {
                To = email,
                Subject = template.Subject,
                Body = template.Body,
                IsHtml = template.IsHtml
            };

            logger.LogInformation("Sending user invitation to {Email} from {Inviter}", email, inviterUsername);
            return await emailService.SendAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send user invitation to {Email}", email);
            return EmailResult.Failure("Failed to send user invitation", ex);
        }
    }
}
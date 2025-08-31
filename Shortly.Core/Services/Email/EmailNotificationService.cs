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
    EmailQueueService emailQueueService,
    IEmailTemplateService templateService,
    IConfiguration configuration,
    ILogger<EmailNotificationService> logger)
    : IEmailNotificationService
{
    /// <inheritdoc />
    public void EnqueueSendEmailVerificationAsync(string email, string userName, string verificationToken)
    {
        var baseUrl = configuration["AppSettings:BaseUrl"];
        var verificationLink = $"{baseUrl}/api/auth/account/verify-email?token={verificationToken}";
        
        var template = templateService.GetEmailVerificationTemplateAsync(userName, verificationLink);

        var emailRequest = new EmailRequest
        {
            To = email,
            Subject = template.Subject,
            Body = template.Body,
            IsHtml = template.IsHtml
        };

        emailQueueService.EnqueueEmail(emailRequest);
        logger.LogInformation("Enqueued email verification to '{Email}'.", email);
    }

    /// <inheritdoc />
    public void EnqueueSendPasswordResetAsync(string email, string userName, string resetToken)
    {
        var baseUrl = configuration["AppSettings:BaseUrl"];
        var resetLink = $"{baseUrl}/reset-password?token={resetToken}&email={Uri.EscapeDataString(email)}";

        var template = templateService.GetPasswordResetTemplateAsync(userName, resetLink);

        var emailRequest = new EmailRequest
        {
            To = email,
            Subject = template.Subject,
            Body = template.Body,
            IsHtml = template.IsHtml
        };

        emailQueueService.EnqueueEmail(emailRequest);
        logger.LogInformation("Enqueued reset password email to {Email}.", email);
        
    }

    /// <inheritdoc />
    public void EnqueueSendUserInvitation(string email, string inviterUsername, string inviteeName, string invitationToken, string organizationName)
    {
        var baseUiUrl = configuration["AppSettings:BaseUIUrl"];
        var invitationLink = $"{baseUiUrl}/authPage.html?token={Uri.EscapeDataString(invitationToken)}";

        var template = templateService.GetUserInvitationTemplateAsync(inviterUsername, inviteeName, invitationLink, organizationName);

        var emailRequest = new EmailRequest
        {
            To = email,
            Subject = template.Subject,
            Body = template.Body,
            IsHtml = template.IsHtml
        };

        emailQueueService.EnqueueEmail(emailRequest);
        logger.LogInformation("Enqueued invitation email for {Email} by {Inviter}.", email, inviterUsername);
    }
}
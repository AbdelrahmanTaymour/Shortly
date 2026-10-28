using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shortly.Core.Abstractions.Email;

namespace Shortly.Infrastructure.Email;

/// <inheritdoc />
/// <remarks>
///     This service leverages <see cref="IEmailTemplates" /> to generate email content
///     and <see cref="IEmailSender" /> to send messages. It builds URLs using configuration
///     values (e.g., <c>AppSettings:BaseUrl</c>) and logs all email operations.
/// </remarks>
internal sealed class EmailNotifier(
    EmailQueueService emailQueueService,
    IEmailTemplates templates,
    IConfiguration configuration,
    ILogger<EmailNotifier> logger)
    : IEmailNotifier
{
    /// <inheritdoc />
    public void EnqueueSendEmailVerificationAsync(string email, string userName, string verificationToken)
    {
        var baseUiUrl = configuration["AppSettings:BaseUIUrl"];
        var verificationLink = $"{baseUiUrl}/account/verify-email?token={verificationToken}";
        
        var template = templates.GetEmailVerificationTemplateAsync(userName, verificationLink);

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
        var baseUiUrl = configuration["AppSettings:BaseUIUrl"];
        var resetLink = $"{baseUiUrl}/account/reset-password?token={resetToken}&email={Uri.EscapeDataString(email)}";

        var template = templates.GetPasswordResetTemplateAsync(userName, resetLink);

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

        var template = templates.GetUserInvitationTemplateAsync(inviterUsername, inviteeName, invitationLink, organizationName);

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

    /// <inheritdoc />
    public void EnqueueEmailChangeConfirmationAsync(string newEmail, string username, string changeToken)
    {
        var baseUiUrl = configuration["AppSettings:BaseUIUrl"];
        var confirmationLink = $"{baseUiUrl}/account/confirm-email-change?token={changeToken}";

        var template = templates.GetEmailChangeConfirmationTemplateAsync(username, newEmail, confirmationLink);

        var emailRequest = new EmailRequest
        {
            To = newEmail,
            Subject = template.Subject,
            Body = template.Body,
            IsHtml = template.IsHtml
        };

        emailQueueService.EnqueueEmail(emailRequest);
        logger.LogInformation("Enqueued email change confirmation to '{NewEmail}' for user '{Username}'.", newEmail, username);
    }
}